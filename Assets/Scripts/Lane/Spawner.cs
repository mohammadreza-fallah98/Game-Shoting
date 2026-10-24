using System.Collections;
using UnityEngine;

// Spawns waves of enemies in random lane groups (lane, count, delay).
public class Spawner : MonoBehaviour
{
    public float spawnZ = 300f;
    public float rowSpacing = 3f;
    public float baseSpeed = 32.721212f;
    public int minEnemies = 500;
    public int maxEnemies = 500;

    public void Begin() { StartCoroutine(Run()); }

    IEnumerator Run()
    {
        var gm = GameManager.Instance;
        yield return new WaitForSeconds(1f);

        while (true)
        {
            int total = Random.Range(minEnemies, maxEnemies + 1);
            float speed = baseSpeed * Mathf.Pow(1.08f, gm.Wave - 1);
            gm.Remaining = total;
            int left = total;

            // Each segment fills some random lanes for `rows` rows; the remaining lanes stay empty.
            while (left > 0)
            {
                int laneCount = GameManager.ActiveLanes;
                int filled = Random.Range(Mathf.Max(1, laneCount - 2), laneCount);
                int rows = Random.Range(3, 9);

                // Shuffle lane indices; the first `filled` are enemy lanes, the rest are empty.
                var lanes = new int[laneCount];
                for (int i = 0; i < lanes.Length; i++) lanes[i] = i;
                for (int i = lanes.Length - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (lanes[i], lanes[j]) = (lanes[j], lanes[i]);
                }

                // Sometimes put a multiplier panel in one of the empty lanes.
                if (Random.value < 0.2f)
                    Gate.Spawn(lanes[Random.Range(filled, lanes.Length)], speed, spawnZ, Gate.RandomKind(), Gate.RandomBonus());

                for (int r = 0; r < rows && left > 0; r++)
                {
                    for (int i = 0; i < filled; i++)
                    {
                        Enemy.Spawn(lanes[i], speed, spawnZ, Enemy.RandomType(gm.Wave));
                        left--;
                    }
                    yield return new WaitForSeconds(rowSpacing / speed);
                }
            }

            // Every few waves a boss closes the wave.
            if (gm.Wave % Enemy.BossEveryWaves == 0)
            {
                int kind = (gm.Wave / Enemy.BossEveryWaves - 1) % Enemy.BossNames.Length;
                gm.ShowBanner("BOSS: " + Enemy.BossNames[kind]);
                yield return new WaitForSeconds(2f);
                Enemy.SpawnBoss(kind, gm.Wave, speed);
            }

            while (Enemy.All.Count > 0) yield return null;
            gm.BeginUpgrade();
            while (gm.State == GameState.Upgrade) yield return null;
            gm.Wave++;
            gm.UpdateLanes();
            yield return new WaitForSeconds(2f);
        }
    }
}
