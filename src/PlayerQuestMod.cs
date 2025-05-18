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
        APGame.Instance.Data.Locations.TryGetValue("Quest: " + E.Quest.Name, out Location loc);
        if (loc != null)
        {
            APGame.Instance.CheckLocation(loc);

            // Goal
            if (
                (APGame.Instance.Data.Goal == 0 && loc.Name == "Quest: Fetch Argyve a Knickknack")
                || (APGame.Instance.Data.Goal == 1 && loc.Name == "Quest: More Than a Willing Spirit")
                || (APGame.Instance.Data.Goal == 2 && loc.Name == "Quest: Decoding the Signal")
            )
            {
                APGame.Instance.SetGoalAchieved();
            }
        }
        return base.HandleEvent(E);
    }
}