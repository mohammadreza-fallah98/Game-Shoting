using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Shop, Missions, Skins, Playing, Upgrade, GameOver, Paused, Settings }

// Builds the whole scene from primitives and holds the game state.
public class GameManager : MonoBehaviour
{
    public const int MaxLanes = 5;
    public const float LaneSpacing = 3.5f;

    // Number of lanes grows with the wave: 2, 3, 4, then 5.
    public const int WavesPerNewLane = 2;
    public static int ActiveLanes = 2;

    public static float LaneToX(int lane) { return (lane - (ActiveLanes - 1) / 2f) * LaneSpacing; }
    public static int LanesForWave(int wave) { return Mathf.Min(MaxLanes, 2 + (wave - 1) / WavesPerNewLane); }

    // Combo: +1 multiplier per 10 kills in a row, max x10. Resets after 4 seconds without a kill.
    const int KillsPerComboStep = 10;
    const int MaxCombo = 10;
    const float ComboTimeout = 4f;

    // Every `MissesPerPenalty` enemies that get past the squad cost `MissPenalty` points.
    const int MissesPerPenalty = 5;
    const int MissPenalty = 5;

    public static GameManager Instance;

    public GameState State = GameState.Menu;
    public int Score;
    public int Kills;
    public int Missed;
    public int RunCoins;
    public int RunXp;
    public int LevelsGained;
    public int Wave = 1;
    public int Remaining;
    public Transform Player;
    public Upgrades.Card[] Cards;

    float runCoinsExact;
    int comboKills;
    float lastKillTime;
    Transform road;
    readonly Transform[] dividers = new Transform[MaxLanes - 1];

    public int ComboMultiplier { get { return Mathf.Min(MaxCombo, 1 + comboKills / KillsPerComboStep); } }
    public float ComboProgress { get { return ComboMultiplier >= MaxCombo ? 1f : (comboKills % KillsPerComboStep) / (float)KillsPerComboStep; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // The runtime-initialize hook runs only once per launch, so scene reloads (Restart) need their own hook.
        SceneManager.sceneLoaded += (scene, mode) => Create();
        Create();
    }

    static void Create()
    {
        if (SceneManager.GetActiveScene().name != "LaneSurvival") return;
        if (FindFirstObjectByType<GameManager>() != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        Instance = this;
        ActiveLanes = LanesForWave(1);
        Gate.Mirrored = false;
        Enemy.Boss = null;
        Upgrades.Reset();
        BuildScene();
        gameObject.AddComponent<Spawner>();
        gameObject.AddComponent<GameUI>();
    }

    float nextAchievementCheck;

    // Esc or P pauses and resumes. Pausing freezes time so the spawner stops too.
    void HandlePauseInput()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !(kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) return;
        if (State == GameState.Playing) Pause();
        else if (State == GameState.Paused) Resume();
    }

    public void Pause()
    {
        State = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    void Update()
    {
        HandlePauseInput();
        if (State == GameState.Playing && Time.time >= nextAchievementCheck)
        {
            nextAchievementCheck = Time.time + 1f;
            Progress.CheckAchievements();
        }
        if (State == GameState.Playing && comboKills > 0 && Time.time - lastKillTime > ComboTimeout)
            BreakCombo();
    }

    public void StartGame()
    {
        State = GameState.Playing;
        Progress.Max("wave", 1);
        GetComponent<Spawner>().Begin();
    }

    // Called by the spawner after the wave number changes; adds lanes when due.
    public void UpdateLanes()
    {
        Progress.Max("wave", Wave);
        int lanes = LanesForWave(Wave);
        if (lanes == ActiveLanes) return;
        ActiveLanes = lanes;
        Gate.ReleaseAll();
        ApplyLaneLayout();
        Effects.Play(Sfx.Upgrade, 0.7f);
        ShowBanner("NEW LANE!");
    }

    // Shows three upgrade cards; the spawner waits until one is chosen.
    public void BeginUpgrade()
    {
        AddCoins(10 * Wave);
        // Every wave starts with the starting squad again; upgrades can add cubes afterwards.
        PlayerController.Instance.SetCount(Meta.StartCubes);
        Cards = Upgrades.Pick3();
        State = GameState.Upgrade;
    }

    public void ChooseUpgrade(int index)
    {
        Cards[index].apply();
        Effects.Play(Sfx.Upgrade, 0.7f);
        State = GameState.Playing;
    }

    // Big centered message shown for a couple of seconds.
    public string BannerText;
    float bannerUntil;
    public bool BannerVisible { get { return Time.time < bannerUntil; } }

    public void ShowBanner(string text)
    {
        BannerText = text;
        bannerUntil = Time.time + 2.5f;
    }

    public void AddBonusCoins(int amount) { AddCoins(amount); }

    public void OpenShop() { State = GameState.Shop; }
    public void CloseShop() { State = GameState.Menu; }

    public void AddKill(int coins)
    {
        Kills++;
        Progress.Add("kills", 1);
        Remaining--;
        Score += ComboMultiplier;
        comboKills++;
        lastKillTime = Time.time;
        RunXp += coins;
        AddCoins(coins);
    }

    public void BreakCombo() { comboKills = 0; }

    // An enemy got past the squad without being killed.
    public void EnemyPassed()
    {
        Remaining--;
        Missed++;
        if (Missed % MissesPerPenalty == 0)
        {
            Score = Mathf.Max(0, Score - MissPenalty);
            Effects.Play(Sfx.GateBad, 0.4f);
        }
    }

    void AddCoins(float amount)
    {
        runCoinsExact += amount * Meta.CoinMult;
        RunCoins = Mathf.FloorToInt(runCoinsExact);
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        Meta.Coins += RunCoins;
        LevelsGained = Meta.AddXp(RunXp);
        if (Score > Meta.BestScore) Meta.BestScore = Score;
        Progress.Max("score", Score);
        Progress.Max("wave", Wave);
        Progress.CheckAchievements();
        PlayerPrefs.Save();
        Effects.Play(Sfx.GameOver, 0.8f);
        Effects.Shake(0.8f, 0.5f);
        GetComponent<Spawner>().StopAllCoroutines();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    static Material baseMaterial;

    // Gives a renderer its own copy of the project material, tinted with `color`.
    // The material lives in Resources so its shader is included in player builds (the default one turns pink).
    public static void Paint(Renderer r, Color color)
    {
        if (baseMaterial == null) baseMaterial = Resources.Load<Material>("LaneMaterial");
        if (baseMaterial != null) r.sharedMaterial = baseMaterial;
        r.material.color = color;
    }

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        Paint(go.GetComponent<Renderer>(), color);
        Destroy(go.GetComponent<Collider>());
        return go;
    }

    public static GameObject MakeCube(string name, Vector3 scale, Color color)
    {
        return Prim(PrimitiveType.Cube, name, Vector3.zero, scale, color);
    }

    public static GameObject MakeShape(PrimitiveType type, string name, Vector3 scale, Color color)
    {
        return Prim(type, name, Vector3.zero, scale, color);
    }

    public static GameObject MakeSphere(string name, Vector3 pos, float size, Color color)
    {
        return Prim(PrimitiveType.Sphere, name, pos, Vector3.one * size, color);
    }

    // Resizes the road, shows the right dividers and widens the player's movement range.
    void ApplyLaneLayout()
    {
        road.localScale = new Vector3(ActiveLanes * LaneSpacing, 0.5f, 400f);
        for (int i = 0; i < dividers.Length; i++)
        {
            bool active = i + 1 < ActiveLanes;
            dividers[i].gameObject.SetActive(active);
            if (active) dividers[i].position = new Vector3((i + 1 - ActiveLanes / 2f) * LaneSpacing, 0.1f, 190f);
        }
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetBounds(ActiveLanes * LaneSpacing / 2f - 0.3f);
    }

    void BuildScene()
    {
        road = Prim(PrimitiveType.Cube, "Road", new Vector3(0f, -0.25f, 190f), new Vector3(1f, 0.5f, 400f), new Color(0.45f, 0.45f, 0.5f)).transform;
        for (int i = 0; i < dividers.Length; i++)
            dividers[i] = Prim(PrimitiveType.Cube, "Divider", new Vector3(0f, 0.1f, 190f), new Vector3(0.1f, 0.2f, 400f), Color.white).transform;

        var light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var cam = new GameObject("Main Camera") { tag = "MainCamera" };
        cam.AddComponent<Camera>().backgroundColor = new Color(0.6f, 0.75f, 0.9f);
        cam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        cam.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0f, 12f, -9f);
        cam.transform.LookAt(new Vector3(0f, 0f, 10f));
        gameObject.AddComponent<Effects>().Init(cam.transform);

        var player = new GameObject("Player");
        player.AddComponent<PlayerController>();
        Player = player.transform;

        ApplyLaneLayout();
    }
}
