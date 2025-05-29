using System;
using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.UI;
using XRL.World;

[Serializable]
public class PersistentData : IComposite
{
    // TODO give these private setters

    public string Host;
    public string Name;
    public string Password;
    public bool LastConnectionSuccessful = false;
    public string Seed;

    public int Goal;
    public int MaxLevel;
    public int LocationsPerLevel;

    public int ItemIndex = 0;

    [NonSerialized] // Custom as Queue does not support Json serialization
    public Queue<Item> DelayedItems = new();
    public Dictionary<string, Location> Locations = new();

    public void Write(SerializationWriter Writer)
    {
        List<Item> list = new(DelayedItems);
        Writer.Write(list);
    }

    public void Read(SerializationReader Reader)
    {
        List<Item> list = Reader.ReadList<Item>();
        DelayedItems = new(list);
    }
}

public static class APLocalOptions
{
    public static bool DelayTrapsInSettlements =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionDelayTrapsInSettlements");
    public static bool PopupOnReceivedItem =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionPopupOnReceivedItem");
    public static bool PopupOnReceivedTrap =>
        Options.GetOptionBool("lonesurv1vor_archipelago_OptionPopupOnReceivedTrap");
}

[Serializable]
public class APGame : IPart
{
    public static APGame Instance => The.Player.GetPart<APGame>();
    public PersistentData Data = new();

    public override bool WantEvent(int ID, int cascade)
    {
        if (ID == BeforeRenderEvent.ID || ID == ZoneActivatedEvent.ID)
            return true;

        return base.WantEvent(ID, cascade);
    }

    public bool Setup()
    {
        try
        {
            Dictionary<string, object> slotData = new();

            while (true)
            {
                APSession.Disconnect();
                APEvents.ClearEvents();

                if (!Data.LastConnectionSuccessful)
                {
                    if (!AskConnectionInfo(out string host, out string name, out string password))
                    {
                        Popup.Show("Missing connection info");
                        return false;
                    }

                    Data.Host = host;
                    Data.Name = name;
                    Data.Password = password;
                }

                Data.LastConnectionSuccessful = APSession.Connect(
                    Data.Host,
                    Data.Name,
                    Data.Password,
                    out slotData,
                    out string[] errors
                );

                if (!Data.LastConnectionSuccessful)
                {
                    GameLog.LogError(
                        $"Couldn't connect to the archipelago server:\n\n{string.Join("\n", errors)}",
                        true
                    );
                    continue;
                }

                if (Data.Seed != null && Data.Seed != APSession.Seed)
                {
                    Data.LastConnectionSuccessful = false;
                    GameLog.LogError(
                        "Incompatible Save: The connected rooms seed has changed. This save game has been used for a different room and is not compatible.",
                        true
                    );
                    continue;
                }

                break;
            }

            Data.Seed = APSession.Seed;

            if (
                !slotData.TryGetValue("goal", out object goal)
                || !slotData.TryGetValue("locations_per_level", out object locationsPerLevel)
            )
            {
                GameLog.LogError("Couldn't fill slot data", true);
                return false;
            }

            Data.Goal = (int)(long)goal;
            Data.MaxLevel = (int)(long)50; // TODO TMP
            Data.LocationsPerLevel = (int)(long)locationsPerLevel;

            SyncLocations();
        }
        catch (Exception E)
        {
            GameLog.DisplayException(E);
            return false;
        }

        return true;
    }

    private bool AskConnectionInfo(out string host, out string name, out string password)
    {
        name = null;
        password = null;

        host = Popup.AskString(
            "Archipelago host?",
            Default: Data.Host ?? "localhost:38281",
            ReturnNullForEscape: true
        );

        if (host == null)
            return false;

        name = Popup.AskString(
            "Archipelago slot name?",
            Default: Data.Name ?? "Player1",
            ReturnNullForEscape: true
        );

        if (name == null)
            return false;

        password = Popup.AskString(
            "Archipelago password?",
            Default: Data.Password ?? "",
            ReturnNullForEscape: true
        );

        if (password == null)
            return false;

        return true;
    }

    public void CheckLocation(Location loc)
    {
        try
        {
            if (loc != null && loc.Check())
            {
                APSession.CheckLocations(loc.Id);
                // Data.Locations.Add(loc.Name, loc);

                GameLog.LogDebug($"Checked location '{loc.Name}'");
            }
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
        }
    }

    public bool HasReceivedItem(string name)
    {
        try
        {
            return APSession.ReceivedItems.Any(it => it.Item2 == name);
        }
        catch (NullReferenceException)
        {
            return false;
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
            return false;
        }
    }

    public void SetGoalAchieved()
    {
        APSession.SetGoalAchieved();
    }

    public void SyncLocations()
    {
        try
        {
            var checkedLocations = Data
                .Locations.Where(l => l.Value.Checked)
                .Select(l => l.Value.Id);
            APSession.CheckLocations(checkedLocations.ToArray());

            Dictionary<string, Location> newLocations = new();
            foreach (var (id, name) in APSession.CheckedLocations)
            {
                var loc = new Location(name, id);
                loc.Check();
                newLocations.Add(name, loc);
            }
            foreach (var (id, name) in APSession.MissingLocations)
            {
                var loc = new Location(name, id);
                newLocations.Add(name, loc);
            }

            Data.Locations = newLocations;

            GameLog.LogDebug(
                $"Locations synced: {APSession.CheckedLocations.Count()}/{Data.Locations.Count()}"
            );
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
        }
    }

    public override bool HandleEvent(BeforeRenderEvent E)
    {
        ProcessMessages();
        ProcessReceivedItems();

        return true;
    }

    public override bool HandleEvent(ZoneActivatedEvent E)
    {
        if (
            !E.Zone.IsWorldMap()
            && (!APLocalOptions.DelayTrapsInSettlements || !E.Zone.IsCheckpoint())
        )
        {
            while (Data.DelayedItems.TryDequeue(out Item item))
            {
                Items.HandleReceivedItem(item);
            }
        }

        return true;
    }

    private void ProcessMessages()
    {
        while (APEvents.Messages.TryDequeue(out QueuedLogMessage m))
        {
            if (m.Popup)
            {
                Popup.Show(m.Message, LogMessage: true);
            }
            else
            {
                XRL.Messages.MessageQueue.AddPlayerMessage(m.Message);
            }
        }
    }

    private void ProcessReceivedItems()
    {
        while (APEvents.ReceivedItems.TryDequeue(out Item item))
        {
            if (item.Index < Data.ItemIndex)
            {
                return;
            }
            else if (item.Index == Data.ItemIndex)
            {
                GameLog.LogDebug($"Skipped all processed items up to index {item.Index}");
                return;
            }

            Items.HandleReceivedItem(item);

            Data.ItemIndex = item.Index;
        }
    }
}
