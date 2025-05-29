using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using XRL;
using XRL.World;

[Serializable]
public class Location : IComposite
{
    public readonly string Name;
    public readonly long Id;
    public bool Checked { get; private set; } = false;

    // Needed for deserialization
    private Location() { }

    public Location(string name, long id)
    {
        Name = name;
        Id = id;
    }

    public bool Check()
    {
        var before = Checked;
        Checked = true;
        return !before;
    }
}

[System.Serializable]
public class StaticLocationDefs
{
    public static readonly Dictionary<string, StaticLocationDefs> Defs = LoadStaticLocationDefs();

    private static Dictionary<string, StaticLocationDefs> LoadStaticLocationDefs()
    {
        string json = File.ReadAllText(
            DataManager.SavePath(
                @"Mods/CavesOfQudArchipelagoRandomizer/Archipelago/worlds/cavesofqud/data/Locations.json"
            )
        );
        var items = JsonConvert.DeserializeObject<List<StaticLocationDefs>>(json);
        return items.ToDictionary(e => e.Name);
    }

    public string Name;
    public string Category;
    public string Type;
    public int Amount = 1;
    public string Blueprint;
}
