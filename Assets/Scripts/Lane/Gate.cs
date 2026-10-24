using System.Collections.Generic;
using UnityEngine;

public enum GateKind { Add, Reset, Halve }

// A standing panel. Green "Xn" adds n cubes, orange "RESET" leaves a single cube,
// purple "/2" halves the squad. It disappears when touched.
public class Gate : MonoBehaviour
{
    static readonly List<Gate> Active = new List<Gate>();

    // While the Mirror boss is alive most panels are bad ones.
    public static bool Mirrored;

    public float speed;
    GateKind kind;
    int bonus;
    TextMesh label;
    Renderer rend;

    // Picks a gate: green 86%, orange 6%, purple 8%.
    public static GateKind RandomKind()
    {
        // Bad panels are 14% of all panels, reduced by the Panel Luck shop upgrade.
        float bad = Mirrored ? 0.7f : 0.14f - Meta.LuckBonus;
        float r = Random.value;
        if (r < 1f - bad) return GateKind.Add;
        return r < 1f - bad * 0.57f ? GateKind.Reset : GateKind.Halve;
    }

    // Higher multipliers are rarer: X1 60%, X2 30%, X3 10%.
    public static int RandomBonus()
    {
        float r = Random.value;
        return r < 0.6f ? 1 : r < 0.9f ? 2 : 3;
    }

    public static void Spawn(int lane, float speed, float z, GateKind kind, int bonus)
    {
        var go = Pool.Get("Gate", () =>
        {
            var g = GameManager.MakeCube("Gate", new Vector3(3.2f, 4f, 0.3f), Color.white);
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(g.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            textGo.transform.localScale = new Vector3(1f / 3.2f, 0.25f, 1f / 0.3f);
            var tm = textGo.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.GetComponent<MeshRenderer>().material = font.material;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 100;
            tm.characterSize = 0.12f;
            tm.color = Color.white;
            var gate = g.AddComponent<Gate>();
            gate.label = tm;
            gate.rend = g.GetComponent<Renderer>();
            return g;
        });
        go.transform.position = new Vector3(GameManager.LaneToX(lane), 2f, z);
        var gt = go.GetComponent<Gate>();
        gt.speed = speed;
        gt.kind = kind;
        gt.bonus = bonus;
        switch (kind)
        {
            case GateKind.Add:
                gt.label.text = "X" + bonus;
                gt.rend.material.color = new Color(0.2f, 0.8f, 0.3f);
                break;
            case GateKind.Reset:
                gt.label.text = "RESET";
                gt.rend.material.color = new Color(1f, 0.5f, 0.1f);
                break;
            default:
                gt.label.text = "/2";
                gt.rend.material.color = new Color(0.6f, 0.2f, 0.9f);
                break;
        }
        go.SetActive(true);
    }

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    // Removes every panel on the road (used when the lane layout changes).
    public static void ReleaseAll()
    {
        foreach (var g in new List<Gate>(Active)) Pool.Release("Gate", g.gameObject);
    }

    // Destroys every panel within `radius` of `center` (bomb explosion).
    public static void DestroyNear(Vector3 center, float radius)
    {
        foreach (var g in new List<Gate>(Active))
        {
            float dx = g.transform.position.x - center.x, dz = g.transform.position.z - center.z;
            if (dx * dx + dz * dz > radius * radius) continue;
            Effects.Burst(g.transform.position, g.rend.material.color, 10);
            Pool.Release("Gate", g.gameObject);
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm.State != GameState.Playing) return;

        transform.position += Vector3.back * speed * Time.deltaTime;
        if (transform.position.z > 1f) return;

        var player = PlayerController.Instance;
        if (player.Overlaps(transform.position.x, 1.75f + Upgrades.magnet))
        {
            bool good = kind == GateKind.Add;
            Progress.Add("panels", 1);
            Effects.Play(good ? Sfx.GateGood : Sfx.GateBad, 0.6f);
            Effects.Burst(transform.position, rend.material.color, 14);
            if (!good)
            {
                Effects.Shake(0.4f, 0.25f);
                gm.BreakCombo();
            }
            switch (kind)
            {
                case GateKind.Add: player.AddCubes(bonus); break;
                case GateKind.Reset: player.SetCount(1); break;
                default: player.SetCount((player.Count + 1) / 2); break;
            }
        }
        Pool.Release("Gate", gameObject);
    }
}
