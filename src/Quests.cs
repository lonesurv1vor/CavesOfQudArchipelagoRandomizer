using System.Linq;
using XRL;
using XRL.World;
using XRL.World.Conversations;
using XRL.World.Conversations.Parts;

namespace APConversations
{
    [HasConversationDelegate]
    public static class APConversationDelegates
    {
        [ConversationDelegate]
        public static bool IfHasReceivedAPItem(DelegateContext Context)
        {
            return APGame.Instance.HasReceivedItem(Context.Value);
        }
    }

    public class AcceptItemDelivery : IConversationPart
    {
        public override bool WantEvent(int ID, int Propagation)
        {
            return base.WantEvent(ID, Propagation) || ID == EnterElementEvent.ID;
        }

        public override bool HandleEvent(EnterElementEvent E)
        {
            E.Element.Elements.Clear();

            foreach (
                var loc in APStaticData
                    .Locations.Where(l => l.Value.Type == "delivery")
                    .OrderBy(l => l.Key)
            )
            {
                if (!APGame.Instance.LocationChecked(loc.Key))
                {
                    var choice = E.Element.AddChoice(null, null, "APDeliveryList");
                    choice.AddPart(new DeliverItem(loc.Key));

                    var item = The.Player.FindObjectInInventory(loc.Value.Blueprint);
                    if (item != null && item.Count >= loc.Value.Amount)
                    {
                        choice.Text =
                            $"{loc.Key} {{{{|&B[{item?.Count ?? 0}/{loc.Value.Amount}]}}}}";
                    }
                    else
                    {
                        choice.Text =
                            $"{loc.Key} {{{{|&O[{item?.Count ?? 0}/{loc.Value.Amount}]}}}}";
                    }
                }
            }

            E.Element.AddChoice(null, "That's all.", "End");

            return base.HandleEvent(E);
        }
    }

    public class DeliverItem : IConversationPart
    {
        public string Location;

        public DeliverItem(string loc)
        {
            Location = loc;
        }

        public override bool WantEvent(int ID, int Propagation)
        {
            return base.WantEvent(ID, Propagation) || ID == EnterElementEvent.ID;
        }

        public override bool HandleEvent(EnterElementEvent E)
        {
            var loc = APStaticData.Locations[Location];
            var item = The.Player.FindObjectInInventory(loc.Blueprint);
            if (item != null && item.Count >= loc.Amount)
            {
                item.Count -= loc.Amount;
                APGame.Instance.CheckLocation(Location);
                return true;
            }
            else
            {
                XRL.UI.Popup.Show("That is not enough.");
                return false;
            }
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
        if (APGame.Instance.IsLocation(E.Quest.Name))
        {
            APGame.Instance.CheckLocation(E.Quest.Name);

            // Goal
            if (
                (
                    APGame.Instance.Data.Goal == 0
                    && E.Quest.Name == "Fetch Argyve a Knickknack~Return to Argyve"
                )
                || (
                    APGame.Instance.Data.Goal == 1
                    && E.Quest.Name == "More Than a Willing Spirit~Return to Grit Gate"
                )
                || (
                    APGame.Instance.Data.Goal == 2
                    && E.Quest.Name == "Decoding the Signal~Return to Grit Gate"
                )
            )
            {
                APGame.Instance.SetGoalAchieved();
            }
        }
        return base.HandleEvent(E);
    }
}
