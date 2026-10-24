using System;
using System.Collections.Generic;
using UnityEngine;

// Daily missions, achievements and cosmetic skins. Everything is saved in PlayerPrefs.
public static class Progress
{
    // ---------- Stats ----------

    // "Max" stats keep the best value, the others are running totals.
    static readonly HashSet<string> MaxStats = new HashSet<string> { "wave", "squad", "score" };
    static readonly string[] AllStats = { "kills", "panels", "boss", "wave", "squad", "score" };

    static string today;
    static float nextDayCheck;

    // Clears the daily counters when the date changes.
    static void EnsureDay()
    {
        if (today != null && Time.realtimeSinceStartup < nextDayCheck) return;
        nextDayCheck = Time.realtimeSinceStartup + 5f;
        today = DateTime.Now.ToString("yyyyMMdd");
        if (PlayerPrefs.GetString("d_date", "") == today) return;

        foreach (var stat in AllStats) PlayerPrefs.DeleteKey("d_" + stat);
        for (int i = 0; i < 3; i++) PlayerPrefs.DeleteKey("dm_claimed_" + i);
        PlayerPrefs.SetString("d_date", today);
    }

    public static void Add(string stat, int n)
    {
        EnsureDay();
        PlayerPrefs.SetInt("d_" + stat, PlayerPrefs.GetInt("d_" + stat, 0) + n);
        PlayerPrefs.SetInt("l_" + stat, PlayerPrefs.GetInt("l_" + stat, 0) + n);
    }

    public static void Max(string stat, int value)
    {
        EnsureDay();
        if (value > PlayerPrefs.GetInt("d_" + stat, 0)) PlayerPrefs.SetInt("d_" + stat, value);
        if (value > PlayerPrefs.GetInt("l_" + stat, 0)) PlayerPrefs.SetInt("l_" + stat, value);
    }

    public static int Daily(string stat) { EnsureDay(); return PlayerPrefs.GetInt("d_" + stat, 0); }
    public static int Lifetime(string stat) { return PlayerPrefs.GetInt("l_" + stat, 0); }

    // ---------- Daily missions ----------

    struct MissionDef
    {
        public string stat, text;
        public int[] targets, rewards;
    }

    static readonly MissionDef[] Defs =
    {
        new MissionDef { stat = "kills",  text = "Defeat {0} enemies",           targets = new[] { 200, 400, 800 },   rewards = new[] { 80, 160, 300 } },
        new MissionDef { stat = "panels", text = "Collect {0} panels",           targets = new[] { 10, 20, 30 },      rewards = new[] { 80, 150, 250 } },
        new MissionDef { stat = "wave",   text = "Reach wave {0}",               targets = new[] { 3, 5, 7 },         rewards = new[] { 100, 200, 350 } },
        new MissionDef { stat = "squad",  text = "Reach a squad of {0} cubes",   targets = new[] { 4, 5, 7 },         rewards = new[] { 100, 200, 350 } },
        new MissionDef { stat = "score",  text = "Score {0} points in one run",  targets = new[] { 300, 800, 1500 },  rewards = new[] { 80, 200, 350 } },
        new MissionDef { stat = "boss",   text = "Defeat {0} boss",              targets = new[] { 1 },               rewards = new[] { 300 } },
    };

    public struct Mission
    {
        public int index;
        public string stat, text;
        public int target, reward;
    }

    static Mission[] missions;
    static string missionsDay;

    // Three different missions, the same for the whole day.
    public static Mission[] TodayMissions()
    {
        EnsureDay();
        if (missions != null && missionsDay == today) return missions;

        var rng = new System.Random(int.Parse(today));
        var pool = new List<MissionDef>(Defs);
        missions = new Mission[3];
        for (int i = 0; i < 3; i++)
        {
            var def = pool[rng.Next(pool.Count)];
            pool.Remove(def);
            int level = rng.Next(def.targets.Length);
            missions[i] = new Mission
            {
                index = i,
                stat = def.stat,
                target = def.targets[level],
                reward = def.rewards[level],
                text = string.Format(def.text, def.targets[level]),
            };
        }
        missionsDay = today;
        return missions;
    }

    public static int MissionProgress(Mission m) { return Mathf.Min(m.target, Daily(m.stat)); }
    public static bool MissionClaimed(Mission m) { EnsureDay(); return PlayerPrefs.GetInt("dm_claimed_" + m.index, 0) == 1; }
    public static bool MissionDone(Mission m) { return Daily(m.stat) >= m.target; }

    public static bool ClaimMission(Mission m)
    {
        if (!MissionDone(m) || MissionClaimed(m)) return false;
        PlayerPrefs.SetInt("dm_claimed_" + m.index, 1);
        Meta.Coins += m.reward;
        PlayerPrefs.Save();
        return true;
    }

    // ---------- Achievements ----------

    public struct Achievement
    {
        public string key, name, description, stat;
        public int target, reward;
    }

    public static readonly Achievement[] Achievements =
    {
        new Achievement { key = "rookie",  name = "Rookie",        description = "Defeat 500 enemies",       stat = "kills",  target = 500,   reward = 100 },
        new Achievement { key = "veteran", name = "Veteran",       description = "Defeat 10000 enemies",     stat = "kills",  target = 10000, reward = 1000 },
        new Achievement { key = "boss",    name = "Boss Slayer",   description = "Defeat a boss",            stat = "boss",   target = 1,     reward = 300 },
        new Achievement { key = "army",    name = "Army",          description = "Reach a squad of 7",       stat = "squad",  target = 7,     reward = 300 },
        new Achievement { key = "survive", name = "Survivor",      description = "Reach wave 10",            stat = "wave",   target = 10,    reward = 500 },
        new Achievement { key = "roller",  name = "High Roller",   description = "Score 2000 in one run",    stat = "score",  target = 2000,  reward = 400 },
        new Achievement { key = "hunter",  name = "Panel Hunter",  description = "Collect 200 panels",       stat = "panels", target = 200,   reward = 200 },
    };

    public static bool IsUnlocked(string key) { return PlayerPrefs.GetInt("ach_" + key, 0) == 1; }

    // Unlocks (and pays) every achievement whose condition is now met.
    public static void CheckAchievements()
    {
        foreach (var a in Achievements)
        {
            if (IsUnlocked(a.key) || Lifetime(a.stat) < a.target) continue;
            PlayerPrefs.SetInt("ach_" + a.key, 1);
            Meta.Coins += a.reward;
            PlayerPrefs.Save();
            if (GameManager.Instance != null) GameManager.Instance.ShowBanner("ACHIEVEMENT: " + a.name);
            Effects.Play(Sfx.Upgrade, 0.7f);
        }
    }

    // ---------- Skins ----------

    public struct Skin
    {
        public string name;
        public Color cube, bullet;
        public int price;
        public string achievement; // optional: unlocked for free by this achievement
    }

    public static readonly Skin[] Skins =
    {
        new Skin { name = "Classic Blue", cube = new Color(0.2f, 0.5f, 1f),   bullet = Color.yellow,                price = 0 },
        new Skin { name = "Lime",         cube = new Color(0.5f, 1f, 0.2f),   bullet = new Color(0.8f, 1f, 0.4f),   price = 300 },
        new Skin { name = "Pink",         cube = new Color(1f, 0.4f, 0.7f),   bullet = new Color(1f, 0.7f, 0.9f),   price = 500 },
        new Skin { name = "Ice",          cube = new Color(0.8f, 0.95f, 1f),  bullet = new Color(0.5f, 0.9f, 1f),   price = 800 },
        new Skin { name = "Gold",         cube = new Color(1f, 0.8f, 0.1f),   bullet = new Color(1f, 0.9f, 0.4f),   price = -1, achievement = "veteran" },
        new Skin { name = "Crimson",      cube = new Color(0.8f, 0.1f, 0.2f), bullet = new Color(1f, 0.4f, 0.3f),   price = -1, achievement = "boss" },
    };

    public static int SelectedSkin
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt("skin_sel", 0), 0, Skins.Length - 1); }
        set { PlayerPrefs.SetInt("skin_sel", value); }
    }

    // Tint for the character picture: the classic skin keeps the original colors.
    public static Color SkinTint { get { return SelectedSkin == 0 ? Color.white : Color.Lerp(Color.white, Skins[SelectedSkin].cube, 0.6f); } }
    public static Color SkinCube { get { return Skins[SelectedSkin].cube; } }
    public static Color SkinBullet { get { return Skins[SelectedSkin].bullet; } }

    public static bool IsSkinOwned(int i)
    {
        var s = Skins[i];
        if (s.price == 0) return true;
        if (s.achievement != null && IsUnlocked(s.achievement)) return true;
        return PlayerPrefs.GetInt("skin_own_" + i, 0) == 1;
    }

    public static bool CanBuySkin(int i) { return !IsSkinOwned(i) && Skins[i].price > 0 && Meta.Coins >= Skins[i].price; }

    public static bool BuySkin(int i)
    {
        if (!CanBuySkin(i)) return false;
        Meta.Coins -= Skins[i].price;
        PlayerPrefs.SetInt("skin_own_" + i, 1);
        PlayerPrefs.Save();
        return true;
    }
}
