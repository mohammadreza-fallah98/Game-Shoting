using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Follows the mouse left/right (A/D and arrows also work), fires with Space or left mouse button.
// The squad is a round cluster of small cubes that slightly overlap; every cube fires.
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    public float moveSpeed = 12f;
    public float minX = -7f;
    public float maxX = 7f;
    public float fireRate = 0.2f;
    public const int MaxCubes = 7;

    // Sunflower (phyllotaxis) packing: cubes fill a disc and overlap a little.
    const float ClusterSpacing = 0.45f;
    const float GoldenAngle = 2.39996f;
    const float UnitHeight = 1.5f;

    readonly List<Transform> cubes = new List<Transform>();
    float targetX;
    float velX;
    float nextFire;

    void Awake()
    {
        Instance = this;
        AddCubes(Meta.StartCubes);
    }

    public int Count { get { return cubes.Count; } }

    public void AddCubes(int n)
    {
        n = Mathf.Min(n, MaxCubes - cubes.Count);
        for (int i = 0; i < n; i++)
        {
            var unit = new GameObject("PlayerUnit");
            unit.transform.SetParent(transform, false);
            Sprites.Build(unit.transform, "player", 1, 0f, UnitHeight)[0].color = Progress.SkinTint;
            cubes.Add(unit.transform);
        }
        Layout();
        Progress.Max("squad", cubes.Count);
    }

    // Recolors the whole squad with the selected skin.
    public void ApplySkin()
    {
        foreach (var c in cubes)
            foreach (var sr in c.GetComponentsInChildren<SpriteRenderer>()) sr.color = Progress.SkinTint;
    }

    // Shrinks the squad to `target` cubes (never below 1).
    public void SetCount(int target)
    {
        target = Mathf.Max(1, target);
        while (cubes.Count > target)
        {
            Destroy(cubes[cubes.Count - 1].gameObject);
            cubes.RemoveAt(cubes.Count - 1);
        }
        Layout();
    }

    public void SetBounds(float halfWidth)
    {
        minX = -halfWidth;
        maxX = halfWidth;
    }

    // Arranges the cubes in a round cluster around the squad position.
    void Layout()
    {
        for (int i = 0; i < cubes.Count; i++)
        {
            float r = ClusterSpacing * Mathf.Sqrt(i);
            float a = i * GoldenAngle;
            cubes[i].localPosition = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }
    }

    // True if any cube is within `halfWidth` (horizontally) of worldX.
    public bool Overlaps(float worldX, float halfWidth)
    {
        foreach (var c in cubes)
            if (Mathf.Abs(c.position.x - worldX) < halfWidth) return true;
        return false;
    }

    // World X where the ray through a screen point meets the ground.
    float GroundX(Vector2 screenPos)
    {
        var ray = Camera.main.ScreenPointToRay(screenPos);
        return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d) ? ray.GetPoint(d).x : targetX;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        var touchscreen = Touchscreen.current;
        if (gm.State != GameState.Playing || Camera.main == null) return;

        // Touch: the squad follows the finger. Mouse: follows the cursor when it moves.
        bool touching = touchscreen != null && touchscreen.primaryTouch.press.isPressed;
        if (touching)
            targetX = GroundX(touchscreen.primaryTouch.position.ReadValue());
        else if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0f)
            targetX = GroundX(mouse.position.ReadValue());

        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) targetX -= moveSpeed * Time.deltaTime;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) targetX += moveSpeed * Time.deltaTime;
        }
        targetX = Mathf.Clamp(targetX, minX, maxX);

        float x = Mathf.SmoothDamp(transform.position.x, targetX, ref velX, 0.05f);
        transform.position = new Vector3(Mathf.Clamp(x, minX, maxX), transform.position.y, transform.position.z);

        // Phones fire automatically; desktop fires with Space or the left mouse button.
        bool fire = Application.isMobilePlatform || touching
            || (kb != null && kb.spaceKey.isPressed)
            || (mouse != null && mouse.leftButton.isPressed);
        if (fire && Time.time >= nextFire)
        {
            nextFire = Time.time + fireRate * Upgrades.fireIntervalMult;
            Effects.Play(Sfx.Shoot, 0.15f);
            foreach (var c in cubes)
                Bullet.Spawn(new Vector3(c.position.x, 1f, c.position.z + 0.3f));
        }
    }
}
