using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using XRL;

[System.Serializable]
public class LocationInfo
{
    public string Name;
    public string Category;
    public string Type;
    public int Amount = 1;
    public string Blueprint;
}

[System.Serializable]
public class ItemInfo
{
    public string Name;
    public string Category;
    public string Type;
    public int Amount = 1;
    public string Blueprint;
}

public static class APStaticData
{
    // The mods folder must not be renamed or this path doesn't match
    private static readonly string _modPath = DataManager.SavePath(
        @"Mods/CavesOfQudArchipelagoRandomizer"
    );

    public static readonly Assembly MultiClient = Assembly.LoadFrom(
        _modPath + @"/thirdparty/Archipelago.MultiClient.Net.dll"
    );

    public static readonly Dictionary<string, LocationInfo> Locations = LoadStaticLocationDefs();

    private static Dictionary<string, LocationInfo> LoadStaticLocationDefs()
    {
        string json = File.ReadAllText(
            _modPath + @"/Archipelago/worlds/cavesofqud/data/Locations.json"
        );
        var items = JsonConvert.DeserializeObject<List<LocationInfo>>(json);
        return items.ToDictionary(e => e.Name);
    }

    public static readonly Dictionary<string, ItemInfo> Items = LoadStaticItemDefs();

    private static Dictionary<string, ItemInfo> LoadStaticItemDefs()
    {
        string json = File.ReadAllText(
            _modPath + @"/Archipelago/worlds/cavesofqud/data/Items.json"
        );
        var items = JsonConvert.DeserializeObject<List<ItemInfo>>(json);
        return items.ToDictionary(e => e.Name);
    }
}
