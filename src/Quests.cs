using System.Linq;
using XRL.UI;
using XRL.World;
using XRL.World.Conversations;

[HasConversationDelegate]
public static class APConversationDelegates
{
    [ConversationDelegate]
    public static bool IfHasReceivedAPItem(DelegateContext Context)
    {
        return APGame.Instance.HasReceivedItem(Context.Value);
    }
}

// TODO namespace
namespace XRL.World.Conversations.Parts
{
    public class AcceptItemDelivery : IConversationPart
    {
        public override bool WantEvent(int ID, int Propagation)
        {
            return base.WantEvent(ID, Propagation) || ID == EnterElementEvent.ID;
        }

        public override bool HandleEvent(EnterElementEvent E)
        {
            E.Element.Elements.Clear();
            foreach (var loc in StaticLocationDefs.Defs.Where(l => l.Value.Type == "delivery"))
            {
                if (!APGame.Instance.Data.Locations[loc.Value.Name].Checked)
                {
                    if (The.Player.HasObjectInInventory(loc.Value.Blueprint, loc.Value.Amount))
                    {
                        var choice = E.Element.AddChoice(loc.Value.Name, $"{loc.Value.Name}", "APDelivery");
                        var ti = new TakeItem(loc.Value.Blueprint)
                        {
                            Amount = $"{loc.Value.Amount}",
                            Destroy = true
                        };
                        choice.AddPart(ti);
                        choice.AddPart(new DeliverItems(loc.Value.Name));
                    }
                    else
                    {
                        var choice = E.Element.AddChoice(loc.Value.Name, $"{loc.Value.Name} {{{{|&O(not enough)}}}}", "APDelivery");
                    }
                }
            }
            return base.HandleEvent(E);
        }
    }

    public class DeliverItems : IConversationPart
    {
        public string LocationName;

        public DeliverItems(string locName)
        {
            LocationName = locName;
        }

        public override bool WantEvent(int ID, int Propagation)
        {
            return base.WantEvent(ID, Propagation) || ID == EnterElementEvent.ID;
        }

        public override bool HandleEvent(EnterElementEvent E)
        {
            APGame.Instance.CheckLocation(APGame.Instance.Data.Locations[LocationName]);
            return base.HandleEvent(E);
        }
    }
}

public class PlayerQuestMod : IPart
{
    public override bool WantEvent(int ID, int cascade)
    {
        return ID == QuestFinishedEvent.ID;
    }

    public override bool HandleEvent(QuestFinishedEvent E)
    {
        APGame.Instance.Data.Locations.TryGetValue(E.Quest.Name, out Location loc);
        if (loc != null)
        {
            APGame.Instance.CheckLocation(loc);

            // Goal
            if (
                (APGame.Instance.Data.Goal == 0 && loc.Name == "Fetch Argyve a Knickknack~Return to Argyve")
                || (APGame.Instance.Data.Goal == 1 && loc.Name == "More Than a Willing Spirit~Return to Grit Gate")
                || (APGame.Instance.Data.Goal == 2 && loc.Name == "Decoding the Signal~Return to Grit Gate")
            )
            {
                APGame.Instance.SetGoalAchieved();
            }
        }
        return base.HandleEvent(E);
    }
}