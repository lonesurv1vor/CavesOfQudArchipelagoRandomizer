using System;
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
