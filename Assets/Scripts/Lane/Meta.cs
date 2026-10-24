using UnityEngine;

// Permanent progress (coins, shop levels, best score) saved in PlayerPrefs.
public static class Meta
{
    public struct Item
    {
        public string key, name, description;
        public int max, baseCost;
    }

    public static readonly Item[] Items =
    {
        new Item { key = "fire",  name = "Fire Rate",      description = "-5% fire interval per level",   max = 10, baseCost = 50 },
        new Item { key = "squad", name = "Starting Squad", description = "+1 starting cube per level",    max = 5,  baseCost = 150 },
        new Item { key = "power", name = "Bullet Power",   description = "+1 bullet damage per level",    max = 3,  baseCost = 200 },
        new Item { key = "luck",  name = "Panel Luck",     description = "-1% bad panels per level",      max = 10, baseCost = 60 },
        new Item { key = "coin",  name = "Coin Bonus",     description = "+10% coins per level",          max = 10, baseCost = 80 },
    };

    public static int Coins
    {
        get { return PlayerPrefs.GetInt("coins", 0); }
        set { PlayerPrefs.SetInt("coins", value); }
    }

    public static int BestScore
    {
        get { return PlayerPrefs.GetInt("best", 0); }
        set { PlayerPrefs.SetInt("best", value); }
    }

    public static int Level(int i) { return PlayerPrefs.GetInt("lvl_" + Items[i].key, 0); }

    public static bool IsMaxed(int i) { return Level(i) >= Items[i].max; }

    // Each level costs about 1.4x the previous one.
    public static int Cost(int i) { return Mathf.RoundToInt(Items[i].baseCost * Mathf.Pow(1.4f, Level(i))); }

    public static bool TryBuy(int i)
    {
        if (IsMaxed(i) || Coins < Cost(i)) return false;
        Coins -= Cost(i);
        PlayerPrefs.SetInt("lvl_" + Items[i].key, Level(i) + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static int PlayerLevel
    {
        get { return PlayerPrefs.GetInt("plvl", 1); }
        set { PlayerPrefs.SetInt("plvl", value); }
    }

    public static int Xp
    {
        get { return PlayerPrefs.GetInt("xp", 0); }
        set { PlayerPrefs.SetInt("xp", value); }
    }

    public static int XpToNext(int level) { return 500 + 250 * (level - 1); }

    // Adds XP, handles level ups (each pays 25 coins x new level) and returns the levels gained.
    public static int AddXp(int amount)
    {
        int xp = Xp + amount;
        int level = PlayerLevel;
        int gained = 0;
        while (xp >= XpToNext(level))
        {
            xp -= XpToNext(level);
            level++;
            gained++;
            Coins += 25 * level;
        }
        Xp = xp;
        PlayerLevel = level;
        return gained;
    }

    public static float FireMult { get { return Mathf.Pow(0.95f, Level(0)); } }
    public static int StartCubes { get { return 1 + Level(1); } }
    public static int PowerBonus { get { return Level(2); } }
    public static float LuckBonus { get { return 0.01f * Level(3); } }
    public static float CoinMult { get { return 1f + 0.1f * Level(4); } }
}
