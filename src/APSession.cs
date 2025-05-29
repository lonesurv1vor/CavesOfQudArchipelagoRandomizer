using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using XRL;

public static class APSession
{
    private static readonly Assembly _assembly = Assembly.LoadFrom(
        DataManager.SavePath(
            @"Mods/CavesOfQudArchipelagoRandomizer/thirdparty/Archipelago.MultiClient.Net.dll"
        )
    );

    private static string _name;
    private static string _password;

    private static object _session;
    private static object _socket;
    private static object _items;
    private static object _locations;
    private static object _messageLog;
    private static object _roomState;

    public static bool Connect(
        string host,
        string name,
        string password,
        out Dictionary<string, object> slotData,
        out string[] errors
    )
    {
        _name = name;
        _password = password;

        var sessionFactory = _assembly.GetType(
            "Archipelago.MultiClient.Net.ArchipelagoSessionFactory"
        );
        _session = sessionFactory
            .GetMethod("CreateSession", new Type[] { typeof(string), typeof(int) })
            .Invoke(null, new[] { host, null });
        _socket = _session.GetType().GetProperty("Socket").GetValue(_session);
        _items = _session.GetType().GetProperty("Items").GetValue(_session);
        _locations = _session.GetType().GetProperty("Locations").GetValue(_session);
        _messageLog = _session.GetType().GetProperty("MessageLog").GetValue(_session);
        _roomState = _session.GetType().GetProperty("RoomState").GetValue(_session);

        return Reconnect(out slotData, out errors);
    }

    public static bool Connected
    {
        get
        {
            if (_socket != null)
            {
                return (bool)_socket.GetType().GetProperty("Connected").GetValue(_socket);
            }

            return false;
        }
    }

    public static string Seed
    {
        get { return (string)_roomState.GetType().GetProperty("Seed").GetValue(_roomState); }
    }

    public static IEnumerable<(long, string)> CheckedLocations
    {
        get
        {
            var allLocationsChecked =
                (IEnumerable<long>)
                    _locations.GetType().GetProperty("AllLocationsChecked").GetValue(_locations);
            return allLocationsChecked.Select(id =>
            {
                var name = (string)
                    _locations
                        .GetType()
                        .GetMethod("GetLocationNameFromId")
                        .Invoke(_locations, new object[] { id, null });
                return (id, name);
            });
        }
    }

    public static IEnumerable<(long, string)> MissingLocations
    {
        get
        {
            var allLocationsChecked =
                (IEnumerable<long>)
                    _locations.GetType().GetProperty("AllMissingLocations").GetValue(_locations);
            return allLocationsChecked.Select(id =>
            {
                var name = (string)
                    _locations
                        .GetType()
                        .GetMethod("GetLocationNameFromId")
                        .Invoke(_locations, new object[] { id, null });
                return (id, name);
            });
        }
    }

    public static IEnumerable<(long, string)> ReceivedItems
    {
        get
        {
            var items =
                (IEnumerable<object>)
                    _items.GetType().GetProperty("AllItemsReceived").GetValue(_items);

            return items.Select(item =>
            {
                var name = (string)item.GetType().GetProperty("ItemName").GetValue(item);
                var id = (long)item.GetType().GetProperty("ItemId").GetValue(item);

                return (id, name);
            });
        }
    }

    public static bool Reconnect(out Dictionary<string, object> slotData, out string[] errors)
    {
        Disconnect();
        AddEventHandlers();

        var loginResult = _session
            .GetType()
            .GetMethod("TryConnectAndLogin")
            .Invoke(
                _session,
                new object[] { "Caves of Qud", _name, 7, null, null, null, _password, true }
            );

        if (loginResult.GetType().Name == "LoginFailure")
        {
            slotData = null;
            errors = (string[])loginResult.GetType().GetProperty("Errors").GetValue(loginResult);
            return false;
        }
        else
        {
            slotData =
                (Dictionary<string, object>)
                    loginResult.GetType().GetProperty("SlotData").GetValue(loginResult);
            errors = null;
            return true;
        }
    }

    public static void Disconnect()
    {
        RemoveEventHandlers();

        if (Connected)
        {
            // TODO unregister helpers here
            ((Task)_socket.GetType().GetMethod("DisconnectAsync").Invoke(_socket, null)).Wait();
        }
    }

    public static void CheckLocations(params long[] ids)
    {
        _locations
            .GetType()
            .GetMethod("CompleteLocationChecks")
            .Invoke(_locations, new object[] { ids });
    }

    public static void SetGoalAchieved()
    {
        _session.GetType().GetMethod("SetGoalAchieved").Invoke(_session, null);
    }

    // private static Delegate _packetReceivedDelegate;
    private static Delegate _onMessageReceivedDelegate;
    private static Delegate _itemReceivedDelegate;
    private static Delegate _errorReceivedDelegate;

    private static void AddEventHandlers()
    {
        // {
        //     var ev = _socket.GetType().GetEvent("PacketReceived");
        //     _packetReceivedDelegate = Delegate.CreateDelegate(ev.EventHandlerType, this,
        //         typeof(APSession).GetMethod("OnPacketReceived",
        //         BindingFlags.Instance | BindingFlags.NonPublic));
        //     ev.AddEventHandler(_socket, _packetReceivedDelegate);
        // }

        {
            var ev = _messageLog.GetType().GetEvent("OnMessageReceived");
            _onMessageReceivedDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType,
                null,
                typeof(APSession).GetMethod(
                    "OnMessageReceived",
                    BindingFlags.Static | BindingFlags.NonPublic
                )
            );
            ev.AddEventHandler(_messageLog, _onMessageReceivedDelegate);
        }

        {
            var ev = _items.GetType().GetEvent("ItemReceived");
            _itemReceivedDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType,
                null,
                typeof(APSession).GetMethod(
                    "OnItemReceived",
                    BindingFlags.Static | BindingFlags.NonPublic
                )
            );
            ev.AddEventHandler(_items, _itemReceivedDelegate);
        }

        {
            var ev = _socket.GetType().GetEvent("ErrorReceived");
            _errorReceivedDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType,
                null,
                typeof(APSession).GetMethod(
                    "OnSocketErrorReceived",
                    BindingFlags.Static | BindingFlags.NonPublic
                )
            );
            ev.AddEventHandler(_socket, _errorReceivedDelegate);
        }
    }

    private static void RemoveEventHandlers()
    {
        if (_session == null)
        {
            return;
        }
        // {
        //     var ev = _messageLog.GetType().GetEvent("OnMessageReceived");
        //     ev.RemoveEventHandler(_messageLog, _packetReceivedDelegate);
        // }

        {
            var ev = _messageLog.GetType().GetEvent("OnMessageReceived");
            ev.RemoveEventHandler(_messageLog, _onMessageReceivedDelegate);
        }

        {
            var ev = _items.GetType().GetEvent("ItemReceived");
            ev.RemoveEventHandler(_items, _itemReceivedDelegate);
        }

        {
            var ev = _socket.GetType().GetEvent("ErrorReceived");
            ev.RemoveEventHandler(_socket, _errorReceivedDelegate);
        }
    }

    // private void OnPacketReceived(object data)
    // {
    //     var type = data.GetType().GetProperty("PacketType").GetValue(data);

    //     APEvents.OnPacketReceived(type.ToString());
    // }

    private static void OnMessageReceived(object data)
    {
        var parts = (IEnumerable<object>)data.GetType().GetProperty("Parts").GetValue(data);

        var message = parts.Select(part =>
        {
            var text = (string)part.GetType().GetProperty("Text").GetValue(part);
            var color = (int?)part.GetType().GetProperty("PaletteColor").GetValue(part);
            var isBackground = (bool)part.GetType().GetProperty("IsBackgroundColor").GetValue(part);

            return (text, color, isBackground);
        });

        APEvents.OnMessageReceived(message);
    }

    private static void OnItemReceived(object data)
    {
        var index = (int)data.GetType().GetProperty("Index").GetValue(data);
        var item = data.GetType().GetMethod("DequeueItem").Invoke(data, null);
        var name = (string)item.GetType().GetProperty("ItemName").GetValue(item);
        var id = (long)item.GetType().GetProperty("ItemId").GetValue(item);

        APEvents.OnItemReceived(id, name, index);
    }

    private static void OnSocketErrorReceived(Exception E, string message)
    {
        APEvents.OnSocketErrorReceived(E, message);
    }
}
