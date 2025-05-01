
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XRL.UI;
using XRL.World;

[Serializable]
public class PersistentData
{
    public string Host;
    public string Name;
    public string Password;
    public string Seed;

    public int ItemIndex = 0;
    public Dictionary<string, Location> Locations = new();
}

[Serializable]
public class APGame : IPart
{
    [SerializeField]
    public PersistentData Data = new();
    [NonSerialized]
    private Dictionary<string, object> SlotData = new();

    public int Goal { get; private set; }
    public int MaxLevel { get; private set; }
    public int LocationsPerLevel { get; private set; }

    public bool Setup()
    {
        try
        {
            if (!AskConnectionInfo(out string host, out string name, out string password))
            {
                Popup.Show("Missing connection info");
                return false;
            }

            Data.Host = host;
            Data.Name = name;
            Data.Password = password;

            APEventData.Clear();

            if (!APSession.Connect(new Uri(Data.Host), Data.Name, Data.Password, out SlotData, out string[] errors))
            {
                GameLog.LogError($"Couldn't connect to the archipelago server:\n\n{string.Join("\n", errors)}", true);
                return false;
            }

            if (Data.Seed != null && Data.Seed != APSession.Seed)
            {
                GameLog.LogError("Incompatible Save: The connected room seed has changed. This save game has been used for a different room and is not compatible.", true);
                return false;
            }
            Data.Seed = APSession.Seed;

            if (!SlotData.TryGetValue("goal", out object goal)
                || !SlotData.TryGetValue("max_level", out object maxLevel)
                || !SlotData.TryGetValue("locations_per_level", out object locationsPerLevel))
            {
                GameLog.LogError("Couldn't fill slot data", true);
                return false;
            }

            Goal = (int)(long)goal;
            MaxLevel = (int)(long)maxLevel;
            LocationsPerLevel = (int)(long)locationsPerLevel;

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
        host = null;
        name = null;
        password = null;

        host = Popup.AskString(
            "Archipelago host?",
            Default: Data.Host ?? "ws://localhost:38281",
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

        return true;
    }

    public void End()
    {
        APEventData.Clear();
        APSession.Disconnect();
    }

    public void CheckLocation(Location loc)
    {
        try
        {
            if (loc != null && loc.Check())
            {
                APSession.CheckLocations(loc.Id);
                GameLog.LogDebug($"Checked location '{loc.Name}'");
            }
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
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
            var checkedLocations = Data.Locations.Where(l => l.Value.Checked).Select(l => l.Value.Id);
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

            GameLog.LogDebug($"Locations synced: {APSession.CheckedLocations.Count()}/{Data.Locations.Count()}");
        }
        catch (Exception e)
        {
            GameLog.DisplayException(e);
        }
    }
}
