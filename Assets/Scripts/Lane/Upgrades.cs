using System;
using System.Collections.Generic;

// Run-based upgrades picked after each wave. Values reset when a new game starts.
public static class Upgrades
{
    public struct Card
    {
        public string name;
        public string description;
        public Action apply;
    }

    public static float fireIntervalMult = 1f;
    public static int damage = 1;
    public static int pierce = 0;
    public static float splashRadius = 0f;
    public static float magnet = 0f;
    public static int shields = 0;

    public static void Reset()
    {
        fireIntervalMult = Meta.FireMult;
        damage = 1 + Meta.PowerBonus;
        pierce = 0;
        splashRadius = 0f;
        magnet = 0f;
        shields = 0;
    }

    static List<Card> BuildAll()
    {
        return new List<Card>
        {
            new Card { name = "Rapid Fire", description = "Fire 15% faster",
                apply = () => fireIntervalMult = Math.Max(0.25f, fireIntervalMult * 0.85f) },
            new Card { name = "Heavy Bullets", description = "Bullets deal +1 damage",
                apply = () => damage++ },
            new Card { name = "Piercing", description = "Bullets pass through +1 enemy",
                apply = () => pierce++ },
            new Card { name = "Explosive", description = "Hits also damage nearby enemies",
                apply = () => splashRadius += 3f },
            new Card { name = "Magnet", description = "Wider panel pickup range",
                apply = () => magnet += 1f },
            new Card { name = "Shield", description = "Absorb one enemy hit",
                apply = () => shields++ },
            new Card { name = "Reinforcements", description = "+2 cubes right now",
                apply = () => PlayerController.Instance.AddCubes(2) },
        };
    }

    // Returns three different random cards.
    public static Card[] Pick3()
    {
        var all = BuildAll();
        var picked = new Card[3];
        for (int i = 0; i < 3; i++)
        {
            int idx = UnityEngine.Random.Range(0, all.Count);
            picked[i] = all[idx];
            all.RemoveAt(idx);
        }
        return picked;
    }
}
