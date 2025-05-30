using UnityEngine;
using XRL;
using XRL.World;
using XRL.World.Parts.Skill;

[System.Serializable]
public class Item : IComposite
{
    public string Name;
    public long Id;
    public int Index;

    // Needed for deserialization
    private Item() { }

    public Item(string name, long id, int index)
    {
        Name = name;
        Id = id;
        Index = index;
    }

    public bool IsTrap()
    {
        if (!APStaticData.Items.ContainsKey(Name))
        {
            return false;
        }
        var def = APStaticData.Items[Name];
        return def != null && def.Category == "trap";
    }

    public bool IsItem()
    {
        if (!APStaticData.Items.ContainsKey(Name))
        {
            return false;
        }
        var def = APStaticData.Items[Name];
        return def != null && def.Type == "item";
    }

    public bool IsLiquid()
    {
        if (!APStaticData.Items.ContainsKey(Name))
        {
            return false;
        }
        var def = APStaticData.Items[Name];
        return def != null && def.Type == "liquid";
    }

    public string Blueprint()
    {
        return APStaticData.Items[Name].Blueprint;
    }

    public int Amount()
    {
        return APStaticData.Items[Name].Amount;
    }
}

public static class Items
{
    public static void HandleReceivedItem(Item item)
    {
        if (item.IsTrap())
        {
            if (The.Player.OnWorldMap())
            {
                APGame.Instance.Data.DelayedItems.Enqueue(item);
                GameLog.LogDebug($"Receive of '{item.Name}' delayed until exiting the world map");
                return;
            }
            else if (
                APLocalOptions.DelayTrapsInSettlements && The.Player.CurrentZone.IsCheckpoint()
            )
            {
                APGame.Instance.Data.DelayedItems.Enqueue(item);
                GameLog.LogDebug($"Receive of '{item.Name}' delayed until exiting the settlement");
                return;
            }

            GameLog.LogGameplay(
                $"{{{{|&RReceived '{item.Name}'}}}}",
                APLocalOptions.PopupOnReceivedTrap
            );

            // TODO improve
            BombTrap(item.Blueprint());
            return;
        }
        else
        {
            GameLog.LogGameplay($"Received '{item.Name}'", APLocalOptions.PopupOnReceivedItem);
        }

        XRL.UI.Popup.Show($"{item.IsItem()} {item.IsLiquid()} {item.Amount()} {item.Blueprint()}");
        if (item.IsItem())
        {
            for (int i = 0; i < item.Amount(); i++)
            {
                The.Player.Inventory.AddObject(item.Blueprint());
            }
            return;
        }
        else if (item.IsLiquid())
        {
            The.Player.GiveDrams(item.Amount(), item.Blueprint());
            return;
        }
        // TODO
        else if (item.Name.StartsWith("Unlock: "))
        {
            // Quest unlock, nothing to do
            return;
        }
        else
        {
            switch (item.Name)
            {
                case "Hit Points":
                    The.Player.GetPart<PlayerStatsMod>().AddHitPoints();
                    return;
                case "Attribute Points":
                    The.Player.GetPart<PlayerStatsMod>().AddAttributePoints();
                    return;
                case "Attribute Bonus":
                    The.Player.GetPart<PlayerStatsMod>().AddAttributeBonus();
                    return;
                case "Mutation Points":
                    The.Player.GetPart<PlayerStatsMod>().AddMutationPoints();
                    return;
                case "Skill Points":
                    The.Player.GetPart<PlayerStatsMod>().AddSkillPoints();
                    return;
                case "Rapid Mutation Advancement":
                    The.Player.GetPart<PlayerStatsMod>().RapidMutationAdvancement();
                    return;
            }
        }

        GameLog.LogError($"Unknown item '{item.Name}' with id {item.Id}");
    }

    private static void BombTrap(string blueprint)
    {
        var amount = Random.Range(1, 6);
        for (var i = 0; i < amount; i++)
        {
            int dist = Random.Range(3, 10);
            int countdown = Random.Range(3, 8);
            var obj = GameObjectFactory.create(blueprint);
            var bomb = Tinkering_LayMine.CreateBomb(obj, The.Player, countdown);
            The.Player.GetCurrentCell().GetRandomLocalAdjacentCellAtRadius(dist).AddObject(bomb);
        }
    }
}
