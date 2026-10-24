using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    struct EnemyType
    {
        public string sprite;
        public float size;
        public int hp;
        public int coins;
        public Color color;
    }

    const int Runner = 0, Brute = 1, Tank = 2, Splitter = 3, Charger = 4, Bomber = 5;
    const int Crusher = 6, Queen = 7, Mirror = 8;

    public const int BossEveryWaves = 3;
    public static readonly string[] BossNames = { "CRUSHER", "SWARM QUEEN", "MIRROR" };

    static readonly EnemyType[] Types =
    {
        // Runner: small cube, 1 hit.
        new EnemyType { sprite = "runner", size = 2.2f, hp = 1, coins = 1, color = new Color(0.9f, 0.2f, 0.2f) },
        // Brute: cylinder, 3 hits.
        new EnemyType { sprite = "brute", size = 3.0f, hp = 3, coins = 3, color = new Color(0.55f, 0.1f, 0.1f) },
        // Tank: big capsule, 8 hits.
        new EnemyType { sprite = "tank", size = 3.4f, hp = 8, coins = 8, color = new Color(0.15f, 0.15f, 0.2f) },
        // Splitter: splits into two Runners when killed.
        new EnemyType { sprite = "splitter", size = 3.0f, hp = 2, coins = 4, color = new Color(0.95f, 0.5f, 0.7f) },
        // Charger: switches into the player's lane when it gets close.
        new EnemyType { sprite = "charger", size = 2.2f, hp = 1, coins = 2, color = new Color(0.1f, 0.8f, 0.8f) },
        // Bomber: explodes when killed, hurting enemies and destroying panels nearby.
        new EnemyType { sprite = "bomber", size = 3.0f, hp = 4, coins = 6, color = new Color(0.7f, 0.9f, 0.1f) },
        // Bosses (size/hp are set in SpawnBoss): wide block, swarm spawner, panel mirror.
        new EnemyType { sprite = "crusher", size = 3.0f, hp = 120, coins = 50, color = new Color(0.85f, 0.1f, 0.1f) },
        new EnemyType { sprite = "queen", size = 5.0f, hp = 150, coins = 50, color = new Color(0.9f, 0.2f, 0.9f) },
        new EnemyType { sprite = "mirror", size = 5.0f, hp = 180, coins = 50, color = new Color(0.85f, 0.85f, 0.95f) },
    };

    // Pictures have empty margins, so they are drawn a bit larger than the hit size.
    const float SpriteScale = 1.55f;

    const float ChargeStartZ = 45f;
    const float ChargeSpeed = 14f;
    const float BombRadius = 6f;
    const int BombDamage = 3;

    public float speed = 32.721212f;
    public static Enemy Boss;

    public float Radius { get; private set; }
    public float HalfWidth { get; private set; }
    public int Hp { get { return hp; } }
    public int MaxHp { get; private set; }
    public string BossName { get { return BossNames[type - Crusher]; } }
    bool IsBoss { get { return type >= Crusher; } }
    public int CoinValue { get { return Types[type].coins; } }

    int type;
    int hp;
    SpriteRenderer[] sprites;
    Color baseColor;
    float flashTimer;
    bool charging, switched;
    float chargeX;
    int stage;
    float minionTimer;
    float minionSpeed;
    float hoverZ;

    // Picks a type. New types unlock with the wave: Splitter at 2, Charger at 3, Bomber at 4.
    public static int RandomType(int wave)
    {
        float r = Random.value;
        if (wave >= 4 && (r -= 0.06f) < 0f) return Bomber;
        if (wave >= 3 && (r -= 0.08f) < 0f) return Charger;
        if (wave >= 2 && (r -= 0.08f) < 0f) return Splitter;
        if ((r -= 0.08f) < 0f) return Tank;
        if ((r -= 0.22f) < 0f) return Brute;
        return Runner;
    }

    public static void Spawn(int lane, float speed, float z, int type)
    {
        SpawnAt(GameManager.LaneToX(lane), z, speed, type);
    }

    static void SpawnAt(float x, float z, float speed, int type)
    {
        var t = Types[type];
        var go = Pool.Get("Enemy" + type, () =>
        {
            var g = new GameObject("Enemy");
            g.AddComponent<Enemy>().sprites = Sprites.Build(g.transform, t.sprite, 1, 0f, t.size * SpriteScale);
            return g;
        });
        go.transform.position = new Vector3(x, 0f, z);
        go.transform.rotation = Sprites.Facing;
        var e = go.GetComponent<Enemy>();
        e.speed = speed;
        e.type = type;
        e.hp = t.hp;
        e.Radius = t.size * 0.5f;
        e.baseColor = t.color;
        e.SetTint(Color.white);
        e.flashTimer = 0f;
        e.charging = false;
        e.switched = false;
        e.HalfWidth = 0f;
        e.MaxHp = t.hp;
        go.SetActive(true);
    }

    // Boss kinds: 0 = Crusher, 1 = Swarm Queen, 2 = Mirror.
    public static void SpawnBoss(int kind, int wave, float waveSpeed)
    {
        int type = Crusher + kind;
        var t = Types[type];
        int stage = Mathf.Max(1, wave / BossEveryWaves);

        var go = Pool.Get("Enemy" + type, () =>
        {
            var g = new GameObject("Boss");
            g.AddComponent<Enemy>();
            return g;
        });

        float x = 0f, z = 60f;
        var e = go.GetComponent<Enemy>();
        e.type = type;
        e.stage = stage;
        e.speed = 6f;
        e.minionSpeed = waveSpeed;
        e.hoverZ = 55f;

        if (kind == 0)
        {
            // Covers every lane but one (one golem per covered lane); the player has to be in the free lane.
            int golems = GameManager.ActiveLanes - 1;
            float width = golems * GameManager.LaneSpacing;
            x = (Random.value < 0.5f ? -1f : 1f) * GameManager.LaneSpacing / 2f;
            e.sprites = Sprites.Build(go.transform, t.sprite, golems, GameManager.LaneSpacing, GameManager.LaneSpacing * SpriteScale);
            e.Radius = 2f;
            e.HalfWidth = width / 2f;
            z = 100f;
        }
        else
        {
            e.sprites = Sprites.Build(go.transform, t.sprite, 1, 0f, 5f * SpriteScale);
            e.Radius = 2.5f;
            e.HalfWidth = 2.5f;
            e.minionTimer = 2f;
        }

        e.MaxHp = Mathf.RoundToInt(t.hp * (1f + 0.8f * (stage - 1)));
        e.hp = e.MaxHp;
        e.baseColor = t.color;
        e.SetTint(Color.white);
        e.flashTimer = 0f;
        go.transform.position = new Vector3(x, 0f, z);
        go.transform.rotation = Sprites.Facing;

        // The boss counts as one enemy of the wave so the remaining counter stays correct.
        GameManager.Instance.Remaining++;
        Boss = e;
        Gate.Mirrored = kind == 2;
        go.SetActive(true);
    }

    void SetTint(Color color)
    {
        if (sprites == null) return;
        foreach (var sr in sprites) sr.color = color;
    }

    // Rectangle test for wide enemies (bosses), circle test for the rest.
    public bool HitTest(Vector3 p, float r)
    {
        var c = transform.position;
        float dx = Mathf.Abs(c.x - p.x), dz = Mathf.Abs(c.z - p.z);
        if (HalfWidth > 0f) return dx < HalfWidth + r && dz < Radius + r;
        return dx * dx + dz * dz < (Radius + r) * (Radius + r);
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        var gm = GameManager.Instance;
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) SetTint(Color.white);
        }
        if (gm.State != GameState.Playing) return;

        if (IsBoss) { UpdateBoss(gm); return; }

        transform.position += Vector3.back * speed * Time.deltaTime;
        if (type == Charger) UpdateCharge();

        // Reaching the player's row hits the player only if they share the same column.
        if (transform.position.z <= 2f && transform.position.z > -2f &&
            PlayerController.Instance.Overlaps(transform.position.x, Radius + 0.15f))
        {
            if (Upgrades.shields > 0)
            {
                // Shield absorbs the hit and destroys the enemy.
                Upgrades.shields--;
                Effects.Burst(transform.position, baseColor, 12);
                Effects.Shake(0.5f, 0.2f);
                gm.Remaining--;
                gm.BreakCombo();
                Release();
            }
            else
            {
                Effects.Burst(transform.position, Color.red, 30);
                gm.GameOver();
            }
        }
        else if (transform.position.z <= -20f)
        {
            gm.EnemyPassed();
            Release();
        }
    }

    void UpdateBoss(GameManager gm)
    {
        var pos = transform.position;

        if (type == Crusher)
        {
            pos.z -= speed * Time.deltaTime;
            transform.position = pos;

            if (pos.z <= 2f && pos.z > -2f && PlayerController.Instance.Overlaps(pos.x, HalfWidth + 0.15f))
            {
                if (Upgrades.shields > 0)
                {
                    // Shield absorbs the crush and knocks the boss back.
                    Upgrades.shields--;
                    gm.BreakCombo();
                    Effects.Shake(0.8f, 0.3f);
                    pos.z += 25f;
                    transform.position = pos;
                }
                else
                {
                    Effects.Burst(pos, Color.red, 40);
                    gm.GameOver();
                }
            }
            else if (pos.z <= -25f)
            {
                // The player dodged it; the boss leaves without a reward.
                gm.Remaining--;
                Release();
            }
            return;
        }

        // Swarm Queen and Mirror: fly in, then hover and sway while throwing out minions.
        if (pos.z > hoverZ) pos.z = Mathf.Max(hoverZ, pos.z - 12f * Time.deltaTime);
        float span = GameManager.ActiveLanes * GameManager.LaneSpacing / 2f - 3f;
        pos.x = Mathf.Sin(Time.time * 0.8f) * span;
        transform.position = pos;

        if (pos.z > hoverZ + 1f) return;
        minionTimer -= Time.deltaTime;
        if (minionTimer > 0f) return;
        minionTimer = type == Queen ? 2.5f : 4f;
        SpawnMinions(gm, pos.z - 3f);
    }

    // One row of Runners with a single free lane.
    void SpawnMinions(GameManager gm, float z)
    {
        int lanes = GameManager.ActiveLanes;
        int free = Random.Range(0, lanes);
        for (int lane = 0; lane < lanes; lane++)
        {
            if (lane == free) continue;
            SpawnAt(GameManager.LaneToX(lane), z, minionSpeed, Runner);
            gm.Remaining++;
        }
    }

    // Once, when close enough, slide sideways into the lane the player is standing in.
    void UpdateCharge()
    {
        var pos = transform.position;
        if (!charging && !switched && pos.z < ChargeStartZ)
        {
            int lanes = GameManager.ActiveLanes;
            float px = PlayerController.Instance.transform.position.x;
            int lane = Mathf.Clamp(Mathf.RoundToInt(px / GameManager.LaneSpacing + (lanes - 1) / 2f), 0, lanes - 1);
            chargeX = GameManager.LaneToX(lane);
            charging = true;
        }
        if (!charging) return;

        pos.x = Mathf.MoveTowards(pos.x, chargeX, ChargeSpeed * Time.deltaTime);
        transform.position = pos;
        if (Mathf.Approximately(pos.x, chargeX)) { charging = false; switched = true; }
    }

    // Returns true if this hit destroyed the enemy.
    public bool Hit(int damage)
    {
        if (!gameObject.activeSelf) return false;

        hp -= damage;
        if (hp > 0)
        {
            // White flash and a tiny debris puff show the hit.
            SetTint(new Color(1f, 0.45f, 0.45f));
            flashTimer = 0.06f;
            Effects.Play(Sfx.Hit, 0.25f);
            Effects.Burst(transform.position, baseColor, 2);
            return false;
        }

        Effects.Play(Sfx.Kill, 0.4f);
        Effects.Burst(transform.position, baseColor, 6 + type * 4);
        if (type == Tank) Effects.Shake(0.3f, 0.2f);
        Release();
        OnDeath();
        return true;
    }

    void OnDeath()
    {
        var pos = transform.position;
        var gm = GameManager.Instance;

        if (IsBoss)
        {
            Progress.Add("boss", 1);
            gm.AddBonusCoins(50 * stage);
            gm.ShowBanner("BOSS DEFEATED!");
            Effects.Burst(pos, baseColor, 60);
            Effects.Play(Sfx.Upgrade, 0.8f);
            Effects.Shake(1f, 0.6f);
            return;
        }

        if (type == Splitter)
        {
            // Two extra Runners count towards the wave so the remaining counter stays correct.
            gm.Remaining += 2;
            float limit = GameManager.ActiveLanes * GameManager.LaneSpacing / 2f - 1f;
            SpawnAt(Mathf.Clamp(pos.x - 1.2f, -limit, limit), pos.z, speed, Runner);
            SpawnAt(Mathf.Clamp(pos.x + 1.2f, -limit, limit), pos.z, speed, Runner);
        }
        else if (type == Bomber)
        {
            Effects.Burst(pos, new Color(1f, 0.6f, 0.1f), 30);
            Effects.Shake(0.5f, 0.25f);
            Gate.DestroyNear(pos, BombRadius);

            foreach (var other in new List<Enemy>(All))
            {
                float dx = other.transform.position.x - pos.x, dz = other.transform.position.z - pos.z;
                if (dx * dx + dz * dz > BombRadius * BombRadius) continue;
                if (other.Hit(BombDamage)) gm.AddKill(other.CoinValue);
            }
        }
    }

    void Release()
    {
        if (IsBoss)
        {
            if (Boss == this) Boss = null;
            Gate.Mirrored = false;
        }
        Pool.Release("Enemy" + type, gameObject);
    }
}
