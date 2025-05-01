using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using XRL;
using XRL.UI;
using XRL.World;


public static class APEventData
{
    public static ConcurrentQueue<QueuedLogMessage> Messages = new();
    public static ConcurrentQueue<Item> ReceivedItems = new();

    public static void Clear()
    {
        Messages.Clear();
        ReceivedItems.Clear();
    }
}

// Everything in here happens in separate thread outside of the game loop
// - it is not allowed to access the games global objects from here directly
public static class APEvents
{
    public static void OnPacketReceived(string message)
    {
        APEventData.Messages.Enqueue(
            new QueuedLogMessage { Message = GameLog.FormatServer(message), Popup = false }
        );
    }

    public static void OnMessageReceived(IEnumerable<(string, int?, bool)> messages)
    {
        try
        {
            string message = "";
            foreach (var (text, color, isBackground) in messages)
            {
                message += GameLog.ApplyAPColor(text, color, isBackground);
            }

            APEventData.Messages.Enqueue(
                new QueuedLogMessage { Message = GameLog.FormatServer(message), Popup = false }
            );
        }
        catch (Exception e)
        {
            DisplayException(e);
        }
    }

    public static void OnItemReceived(long id, string name, int index)
    {
        try
        {
            APEventData.ReceivedItems.Enqueue(new Item(name, id, index));
        }
        catch (Exception e)
        {
            DisplayException(e);
        }
    }

    public static void OnSocketErrorReceived(Exception E, string message)
    {
        AddErrorMessage($"Connection to the Archipelago server has been lost. You can continue playing offline, progress will be synced after reconnecting. To reconnect, please save and quit to main menu and reload the saved game.", true);
    }

    private static void DisplayException(Exception e)
    {
        AddErrorMessage($"EXCEPTION: {e}", true);
    }

    private static void AddErrorMessage(string message, bool popup)
    {
        APEventData.Messages.Enqueue(new QueuedLogMessage { Message = GameLog.FormatError(message), Popup = popup });
    }
}


public class APEventsProcessor : IPart
{
    public override bool WantEvent(int ID, int cascade)
    {
        if (ID == BeforeRenderEvent.ID)
            return true;

        return base.WantEvent(ID, cascade);
    }

    public override bool HandleEvent(BeforeRenderEvent E)
    {
        var aps = The.Player.GetPart<APGame>();
        ProcessMessages(aps);
        ProcessReceivedItems(aps);

        return true;
    }

    private void ProcessMessages(APGame aps)
    {
        while (APEventData.Messages.TryDequeue(out QueuedLogMessage m))
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

    private void ProcessReceivedItems(APGame aps)
    {
        while (APEventData.ReceivedItems.TryDequeue(out Item item))
        {
            if (item.Index < aps.Data.ItemIndex)
            {
                return;
            }
            else if (item.Index == aps.Data.ItemIndex)
            {
                GameLog.LogDebug($"Skipped all processed items up to index {item.Index}");
                return;
            }

            Items.HandleReceivedItem(item);

            aps.Data.ItemIndex = item.Index;
        }
    }
}
