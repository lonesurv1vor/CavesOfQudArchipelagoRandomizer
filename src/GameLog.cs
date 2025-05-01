using System;
using XRL.UI;

public struct QueuedLogMessage
{
    public string Message;
    public bool Popup;
}

public static class GameLog
{
    public static string FormatServer(string message)
    {
        return $"{{{{|&O[AP Server]}}}} {message}";
    }

    public static string FormatGameplay(string message)
    {
        return $"{{{{|&O[AP]}}}} {message}";
    }

    public static string FormatDebug(string message)
    {
        return $"{{{{|&w[AP Debug]}}}} {message}";
    }

    public static string FormatError(string message)
    {
        return $"{{{{|&R[AP Error] {message}}}}}";
    }

    public static string ApplyAPColor(string text, int? color, bool isBackground)
    {
        string res = "{{|" + (isBackground ? "^" : "&");
        res += color switch
        {
            0 => "y",
            1 => "K",
            2 => "R",
            3 => "g",
            4 => "b",
            5 => "C",
            6 => "M",
            7 => "W",
            8 => "B",
            9 => "r",
            10 => "m",
            _ => "y"
        };
        res += text + "}}";

        return res;
    }

    public static void LogGameplay(string message)
    {
        XRL.Messages.MessageQueue.AddPlayerMessage(GameLog.FormatGameplay(message));
    }

    public static void LogDebug(string message)
    {
        XRL.Messages.MessageQueue.AddPlayerMessage(GameLog.FormatDebug(message));
    }

    public static void LogError(string message, bool popup = false)
    {
        if (popup)
        {
            Popup.Show(FormatError(message), LogMessage: true);
        }
        else
        {
            XRL.Messages.MessageQueue.AddPlayerMessage(FormatError(message));
        }
    }

    public static void DisplayException(Exception E)
    {
        var text = $"EXCEPTION: {E}";
        Popup.Show(FormatError(text), LogMessage: true);
    }
}