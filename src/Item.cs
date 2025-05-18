using Qud.API;
using XRL;
using XRL.World;
using XRL.World.Parts.Skill;
using UnityEngine;
using GameObject = XRL.World.GameObject;
using System.Globalization;


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
}

public static class Items
{
    public static void HandleReceivedItem(Item item)
    {
        if (item.Name.Contains("trap", CompareOptions.IgnoreCase))
        {
            if (The.Player.OnWorldMap())
            {
                APGame.Instance.Data.DelayedItems.Enqueue(item);
                GameLog.LogDebug($"Receive of '{item.Name}' delayed until exiting the world map");
                return;
            }
            else if (APLocalOptions.DelayTrapsInSettlements && The.Player.CurrentZone.IsCheckpoint())
            {
                APGame.Instance.Data.DelayedItems.Enqueue(item);
                GameLog.LogDebug($"Receive of '{item.Name}' delayed until exiting the settlement");
                return;
            }

            GameLog.LogGameplay($"{{{{|&RReceived '{item.Name}'}}}}", APLocalOptions.PopupOnReceivedTrap);
        }
        else
        {
            GameLog.LogGameplay($"Received '{item.Name}'", APLocalOptions.PopupOnReceivedItem);
        }

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
            case "10 Drams of Fresh Water":
                The.Player.GiveDrams(10, "water");
                return;
            case "Spawn Creature Trap":
                The.Player.GetCurrentCell().GetRandomLocalAdjacentCellAtRadius(4).AddObject(EncountersAPI.GetCreatureAroundLevel(The.Player.Level + 3));
                return;
            case "Bomb Trap":
                BombTrap();
                return;
            case "Double Bomb Trap":
                BombTrap();
                BombTrap();
                return;
        }

        if (item.Name.StartsWith("Item: "))
        {
            var split = item.Name.Split(" ");
            if (split.Length >= 2)
            {
                string blueprint;
                if (int.TryParse(split[1], out int amount))
                {
                    blueprint = string.Join(" ", split[2..]);
                }
                else
                {
                    amount = 1;
                    blueprint = string.Join(" ", split[1..]);
                }

                for (int i = 0; i < amount; i++)
                {
                    The.Player.Inventory.AddObject(blueprint);
                }

                return;
            }
        }

        GameLog.LogError($"Unknown item '{item.Name}' with id {item.Id}");
    }

    private static void BombTrap()
    {
        int dist = Random.Range(2, 6);
        int countdown = Random.Range(2, 5);
        var bomb = Tinkering_LayMine.CreateBomb((GameObject)null, The.Player, countdown);
        The.Player.GetCurrentCell().GetRandomLocalAdjacentCellAtRadius(dist).AddObject(bomb);
    }
}