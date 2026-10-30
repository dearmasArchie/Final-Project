using System.Collections.Generic;
using UnityEngine;

namespace SignalLost
{
    /// <summary>Spawns alerted enemies when an alarm goes off (laser tripwire, wrong breaker), with a cap on how many are alive.</summary>
    public class AlarmSpawner : MonoBehaviour
    {
        [SerializeField] Transform[] spawnPoints;
        [SerializeField] int maxAlive = 2;

        readonly List<GameObject> alive = new List<GameObject>();

        public void Spawn(GameObject enemyPrefab)
        {
            alive.RemoveAll(e => e == null);
            if (alive.Count >= maxAlive || spawnPoints.Length == 0) return;

            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var enemy = Instantiate(enemyPrefab, point.position, point.rotation);
            if (enemy.TryGetComponent<EnemyAI>(out var ai)) ai.AlertOnSpawn();
            alive.Add(enemy);
        }
    }
}
