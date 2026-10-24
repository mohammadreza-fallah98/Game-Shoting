using UnityEngine;

// Minimal IMGUI HUD, menu, shop, upgrade cards and game over screen.
public class GameUI : MonoBehaviour
{
    GUIStyle label, button;

    void OnGUI()
    {
        var gm = GameManager.Instance;
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            label.normal.textColor = Color.white;
        }
        // Fit both landscape (height-limited) and portrait phones (width-limited).
        float s = Mathf.Min(Screen.height / 600f, Screen.width / 780f);
        label.fontSize = Mathf.RoundToInt(28 * s);
        button.fontSize = Mathf.RoundToInt(28 * s);
        button.wordWrap = false;
        float w = Screen.width, cx = w / 2f;

        switch (gm.State)
        {
            case GameState.Playing:
                DrawHud(gm, w, cx);
                break;

            case GameState.Upgrade:
                DrawUpgradeCards(gm, s, w, cx);
                break;

            case GameState.Shop:
                DrawShop(gm, s, w, cx);
                break;

            case GameState.Paused:
                GUI.color = new Color(0f, 0f, 0f, 0.6f);
                GUI.DrawTexture(new Rect(0, 0, w, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(0, 150 * s, w, 60 * s), "PAUSED", label);
                if (GUI.Button(new Rect(cx - 110 * s, 240 * s, 220 * s, 60 * s), "RESUME", button)) gm.Resume();
                if (GUI.Button(new Rect(cx - 110 * s, 320 * s, 220 * s, 60 * s), "MAIN MENU", button)) gm.Restart();
                break;

            case GameState.Settings:
                DrawSettings(gm, s, w, cx);
                break;

            case GameState.Missions:
                DrawMissions(gm, s, w, cx);
                break;

            case GameState.Skins:
                DrawSkins(gm, s, w, cx);
                break;

            case GameState.Menu:
                GUI.Label(new Rect(0, 120 * s, w, 60 * s), "LANE SURVIVAL", label);
                GUI.Label(new Rect(0, 180 * s, w, 40 * s), $"Coins: {Meta.Coins}    Best: {Meta.BestScore}", label);
                GUI.Label(new Rect(0, 215 * s, w, 30 * s), $"Level {Meta.PlayerLevel}   XP {Meta.Xp}/{Meta.XpToNext(Meta.PlayerLevel)}", label);
                DrawBar(new Rect(cx - 150 * s, 245 * s, 300 * s, 8 * s), Meta.Xp / (float)Meta.XpToNext(Meta.PlayerLevel), new Color(0.3f, 0.8f, 1f));
                if (GUI.Button(new Rect(cx - 100 * s, 265 * s, 200 * s, 55 * s), "PLAY", button)) gm.StartGame();
                if (GUI.Button(new Rect(cx - 100 * s, 330 * s, 200 * s, 55 * s), "SHOP", button)) gm.OpenShop();
                if (GUI.Button(new Rect(cx - 100 * s, 395 * s, 200 * s, 55 * s), "MISSIONS", button)) gm.State = GameState.Missions;
                if (GUI.Button(new Rect(cx - 100 * s, 460 * s, 200 * s, 55 * s), "SKINS", button)) gm.State = GameState.Skins;

                button.fontSize = Mathf.RoundToInt(20 * s);
                if (GUI.Button(new Rect(cx - 150 * s, 530 * s, 140 * s, 45 * s), "SETTINGS", button)) gm.State = GameState.Settings;
                if (GUI.Button(new Rect(cx + 10 * s, 530 * s, 140 * s, 45 * s), "QUIT", button)) QuitGame();
                button.fontSize = Mathf.RoundToInt(28 * s);
                var hint = new GUIStyle(label) { fontSize = Mathf.RoundToInt(16 * s) };
                GUI.Label(new Rect(0, 80 * s, w, 30 * s), "Mouse / A-D: move    Click / Space: fire    Esc: pause", hint);
                break;

            default:
                GUI.Label(new Rect(0, 110 * s, w, 60 * s), "GAME OVER", label);
                GUI.Label(new Rect(0, 170 * s, w, 50 * s), $"Enemies Defeated: {gm.Kills}    Score: {gm.Score}", label);
                GUI.Label(new Rect(0, 220 * s, w, 50 * s), $"Coins earned: {gm.RunCoins}    Total: {Meta.Coins}", label);
                GUI.Label(new Rect(0, 270 * s, w, 50 * s),
                    $"XP +{gm.RunXp}    Level {Meta.PlayerLevel}" + (gm.LevelsGained > 0 ? "   LEVEL UP!" : ""), label);
                if (GUI.Button(new Rect(cx - 100 * s, 340 * s, 200 * s, 60 * s), "RESTART", button)) gm.Restart();
                break;
        }
    }

    // The HUD has its own scale and three short lines so it stays readable on narrow (portrait) screens.
    void DrawHud(GameManager gm, float w, float cx)
    {
        float hs = Mathf.Min(Screen.height / 600f, Screen.width / 520f);
        label.fontSize = Mathf.RoundToInt(28 * hs);
        GUI.Label(new Rect(0, 8 * hs, w, 36 * hs), $"Score {gm.Score}   Wave {gm.Wave}", label);
        GUI.Label(new Rect(0, 42 * hs, w, 36 * hs), $"Enemies {gm.Remaining}   Squad {PlayerController.Instance.Count}", label);
        GUI.Label(new Rect(0, 76 * hs, w, 36 * hs), $"Coins {gm.RunCoins}   x{gm.ComboMultiplier}   Miss {gm.Missed}", label);
        DrawBar(new Rect(cx - 150 * hs, 116 * hs, 300 * hs, 10 * hs), gm.ComboProgress, new Color(1f, 0.8f, 0.1f));

        if (Enemy.Boss != null && Enemy.Boss.gameObject.activeSelf)
        {
            GUI.Label(new Rect(0, 130 * hs, w, 36 * hs), Enemy.Boss.BossName, label);
            DrawBar(new Rect(cx - 240 * hs, 168 * hs, 480 * hs, 16 * hs), Enemy.Boss.Hp / (float)Enemy.Boss.MaxHp, new Color(0.9f, 0.1f, 0.1f));
        }

        if (gm.BannerVisible)
            GUI.Label(new Rect(0, 260 * hs, w, 80 * hs), gm.BannerText, label);

        // On-screen pause button (phones have no Esc key).
        button.fontSize = Mathf.RoundToInt(26 * hs);
        if (GUI.Button(new Rect(w - 62 * hs, 8 * hs, 54 * hs, 44 * hs), "II", button)) gm.Pause();
    }

    static void DrawBar(Rect r, float t, Color color)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawUpgradeCards(GameManager gm, float s, float w, float cx)
    {
        GUI.Label(new Rect(0, 100 * s, w, 60 * s), $"WAVE {gm.Wave} CLEARED - CHOOSE AN UPGRADE", label);
        float cardW = 240 * s, gap = 20 * s;
        float x0 = cx - (3 * cardW + 2 * gap) / 2f;
        button.fontSize = Mathf.RoundToInt(22 * s);
        button.wordWrap = true;
        for (int i = 0; i < gm.Cards.Length; i++)
        {
            var card = gm.Cards[i];
            string text = card.name + "\n\n" + card.description;
            if (GUI.Button(new Rect(x0 + i * (cardW + gap), 200 * s, cardW, 200 * s), text, button))
            {
                gm.ChooseUpgrade(i);
                break;
            }
        }
    }

    static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void DrawSettings(GameManager gm, float s, float w, float cx)
    {
        GUI.Label(new Rect(0, 100 * s, w, 50 * s), "SETTINGS", label);

        GUI.Label(new Rect(cx - 300 * s, 190 * s, 200 * s, 40 * s), "Volume", label);
        float volume = GUI.HorizontalSlider(new Rect(cx - 80 * s, 205 * s, 300 * s, 20 * s), AudioListener.volume, 0f, 1f);
        if (!Mathf.Approximately(volume, AudioListener.volume))
        {
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat("volume", volume);
        }

        button.fontSize = Mathf.RoundToInt(22 * s);
        string mode = Screen.fullScreen ? "FULLSCREEN: ON" : "FULLSCREEN: OFF";
        if (GUI.Button(new Rect(cx - 150 * s, 260 * s, 300 * s, 55 * s), mode, button)) Screen.fullScreen = !Screen.fullScreen;

        button.fontSize = Mathf.RoundToInt(28 * s);
        if (GUI.Button(new Rect(cx - 100 * s, 360 * s, 200 * s, 55 * s), "BACK", button))
        {
            PlayerPrefs.Save();
            gm.State = GameState.Menu;
        }
    }

    void DrawMissions(GameManager gm, float s, float w, float cx)
    {
        GUI.Label(new Rect(0, 15 * s, w, 40 * s), $"DAILY MISSIONS    Coins: {Meta.Coins}", label);
        button.fontSize = Mathf.RoundToInt(20 * s);
        var missions = Progress.TodayMissions();
        for (int i = 0; i < missions.Length; i++)
        {
            var m = missions[i];
            float y = 65 * s + i * 60 * s;
            GUI.Label(new Rect(cx - 420 * s, y, 520 * s, 50 * s),
                $"{m.text}  ({Progress.MissionProgress(m)}/{m.target})   +{m.reward} coins", label);

            bool claimed = Progress.MissionClaimed(m);
            GUI.enabled = Progress.MissionDone(m) && !claimed;
            if (GUI.Button(new Rect(cx + 120 * s, y + 5 * s, 200 * s, 45 * s), claimed ? "CLAIMED" : "CLAIM", button))
                Progress.ClaimMission(m);
            GUI.enabled = true;
        }

        GUI.Label(new Rect(0, 255 * s, w, 40 * s), "ACHIEVEMENTS", label);
        var small = new GUIStyle(label) { fontSize = Mathf.RoundToInt(18 * s) };
        for (int i = 0; i < Progress.Achievements.Length; i++)
        {
            var a = Progress.Achievements[i];
            bool done = Progress.IsUnlocked(a.key);
            small.normal.textColor = done ? new Color(0.5f, 1f, 0.5f) : new Color(0.8f, 0.8f, 0.8f);
            GUI.Label(new Rect(0, 295 * s + i * 27 * s, w, 26 * s),
                $"{(done ? "[x]" : "[ ]")} {a.name}: {a.description}  (+{a.reward} coins)", small);
        }

        button.fontSize = Mathf.RoundToInt(28 * s);
        if (GUI.Button(new Rect(cx - 100 * s, 510 * s, 200 * s, 55 * s), "BACK", button)) gm.State = GameState.Menu;
    }

    void DrawSkins(GameManager gm, float s, float w, float cx)
    {
        GUI.Label(new Rect(0, 15 * s, w, 40 * s), $"SKINS    Coins: {Meta.Coins}", label);
        button.fontSize = Mathf.RoundToInt(20 * s);
        for (int i = 0; i < Progress.Skins.Length; i++)
        {
            var skin = Progress.Skins[i];
            float y = 70 * s + i * 65 * s;

            GUI.color = skin.cube;
            GUI.DrawTexture(new Rect(cx - 380 * s, y + 8 * s, 40 * s, 40 * s), Texture2D.whiteTexture);
            GUI.color = skin.bullet;
            GUI.DrawTexture(new Rect(cx - 330 * s, y + 18 * s, 20 * s, 20 * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(cx - 300 * s, y, 360 * s, 55 * s), skin.name, label);

            bool owned = Progress.IsSkinOwned(i);
            bool selected = Progress.SelectedSkin == i;
            string text;
            if (selected) text = "SELECTED";
            else if (owned) text = "SELECT";
            else if (skin.price > 0) text = $"BUY {skin.price}";
            else text = "LOCKED";

            GUI.enabled = !selected && (owned || Progress.CanBuySkin(i));
            if (GUI.Button(new Rect(cx + 120 * s, y + 5 * s, 200 * s, 45 * s), text, button))
            {
                if (!owned) Progress.BuySkin(i);
                Progress.SelectedSkin = i;
                PlayerPrefs.Save();
                if (PlayerController.Instance != null) PlayerController.Instance.ApplySkin();
            }
            GUI.enabled = true;
        }

        button.fontSize = Mathf.RoundToInt(28 * s);
        if (GUI.Button(new Rect(cx - 100 * s, 480 * s, 200 * s, 55 * s), "BACK", button)) gm.State = GameState.Menu;
    }

    void DrawShop(GameManager gm, float s, float w, float cx)
    {
        GUI.Label(new Rect(0, 20 * s, w, 50 * s), $"SHOP    Coins: {Meta.Coins}", label);
        button.fontSize = Mathf.RoundToInt(22 * s);
        for (int i = 0; i < Meta.Items.Length; i++)
        {
            var item = Meta.Items[i];
            float y = 90 * s + i * 70 * s;
            GUI.Label(new Rect(cx - 420 * s, y, 520 * s, 60 * s),
                $"{item.name}  (Lv {Meta.Level(i)}/{item.max})\n{item.description}", label);

            string btn = Meta.IsMaxed(i) ? "MAX" : $"BUY {Meta.Cost(i)}";
            GUI.enabled = !Meta.IsMaxed(i) && Meta.Coins >= Meta.Cost(i);
            if (GUI.Button(new Rect(cx + 120 * s, y + 5 * s, 200 * s, 50 * s), btn, button)) Meta.TryBuy(i);
            GUI.enabled = true;
        }
        button.fontSize = Mathf.RoundToInt(28 * s);
        if (GUI.Button(new Rect(cx - 100 * s, 90 * s + Meta.Items.Length * 70 * s + 20 * s, 200 * s, 60 * s), "BACK", button)) gm.CloseShop();
    }
}
