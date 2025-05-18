using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


// Everything in here happens in separate thread outside of the game loop
// - it is not allowed to access the games global objects from here directly
public static class APEvents
{
    public static ConcurrentQueue<QueuedLogMessage> Messages = new();
    public static ConcurrentQueue<Item> ReceivedItems = new();

    public static void ClearEvents()
    {
        Messages.Clear();
        ReceivedItems.Clear();
    }

    public static void OnPacketReceived(string message)
    {
        Messages.Enqueue(
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

            Messages.Enqueue(
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
            ReceivedItems.Enqueue(new Item(name, id, index));
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
        Messages.Enqueue(new QueuedLogMessage { Message = GameLog.FormatError(message), Popup = popup });
    }
}
