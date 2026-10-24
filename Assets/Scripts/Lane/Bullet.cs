using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 100f;
    public float maxLifetime = 3f;
    const float BulletRadius = 0.15f;

    static readonly List<Enemy> splashTargets = new List<Enemy>();

    readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();
    float lifetime;
    int damage;
    int pierceLeft;

    public static void Spawn(Vector3 pos)
    {
        var go = Pool.Get("Bullet", () =>
        {
            var g = GameManager.MakeSphere("Bullet", Vector3.zero, 0.3f, Progress.SkinBullet);
            g.AddComponent<Bullet>();
            return g;
        });
        go.transform.position = pos;
        var b = go.GetComponent<Bullet>();
        b.lifetime = b.maxLifetime;
        b.damage = Upgrades.damage;
        b.pierceLeft = Upgrades.pierce;
        b.alreadyHit.Clear();
        go.SetActive(true);
    }

    // Height is ignored so the raised bullet still hits enemies on the ground.
    static float HorizontalDistSqr(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        transform.position += Vector3.forward * speed * Time.deltaTime;
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) { Pool.Release("Bullet", gameObject); return; }

        for (int i = Enemy.All.Count - 1; i >= 0; i--)
        {
            var e = Enemy.All[i];
            if (alreadyHit.Contains(e)) continue;
            if (!e.HitTest(transform.position, BulletRadius)) continue;

            alreadyHit.Add(e);
            Vector3 hitPos = e.transform.position;
            DealDamage(e, damage);
            Splash(hitPos, e);

            if (pierceLeft > 0) { pierceLeft--; continue; }
            Pool.Release("Bullet", gameObject);
            return;
        }
    }

    static void DealDamage(Enemy e, int dmg)
    {
        if (e.Hit(dmg)) GameManager.Instance.AddKill(e.CoinValue);
    }

    // Explosive upgrade: one point of damage to enemies near the hit.
    static void Splash(Vector3 center, Enemy source)
    {
        float r = Upgrades.splashRadius;
        if (r <= 0f) return;

        splashTargets.Clear();
        foreach (var other in Enemy.All)
            if (other != source && HorizontalDistSqr(other.transform.position, center) < r * r)
                splashTargets.Add(other);
        foreach (var other in splashTargets) DealDamage(other, 1);
    }
}
