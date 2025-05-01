using System;

[Serializable]
public class Location
{
    public readonly string Name;
    public readonly long Id;
    public bool Checked { get; private set; } = false;

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
