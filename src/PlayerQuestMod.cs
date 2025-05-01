using XRL;
using XRL.World;

public class PlayerQuestMod : IPart
{
    public override bool WantEvent(int ID, int cascade)
    {
        return ID == QuestFinishedEvent.ID;
    }

    public override bool HandleEvent(QuestFinishedEvent E)
    {
        var aps = The.Player.GetPart<APGame>();
        aps.Data.Locations.TryGetValue("Quest: " + E.Quest.Name, out Location loc);
        if (loc != null)
        {
            aps.CheckLocation(loc);

            // Goal
            if (
                (aps.Goal == 0 && loc.Name == "Quest: Fetch Argyve a Knickknack")
                || (aps.Goal == 1 && loc.Name == "Quest: More Than a Willing Spirit")
                || (aps.Goal == 2 && loc.Name == "Quest: Decoding the Signal")
            )
            {
                aps.SetGoalAchieved();
            }
        }
        return base.HandleEvent(E);
    }
}