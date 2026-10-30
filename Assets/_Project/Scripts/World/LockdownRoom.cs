using System.Collections.Generic;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Wing B quarantine ambush. Begin() (called when Fuse 2 is taken) seals the doors, starts a 45-second
    /// countdown, and spawns the squad. The doors unlock when the squad is destroyed or time runs out.
    /// </summary>
    public class LockdownRoom : MonoBehaviour
    {
        [SerializeField] Door[] doors;
        [SerializeField] Transform[] spawnPoints;
        [Tooltip("One prefab per spawn point, in the same order.")]
        [SerializeField] GameObject[] squad;
        [SerializeField] float duration = 45f;

        readonly List<GameObject> alive = new List<GameObject>();
        float timeLeft;

        public bool Running { get; private set; }

        public void Begin()
        {
            if (Running) return;
            Running = true;
            timeLeft = duration;

            foreach (var door in doors)
            {
                door.Close();
                door.Lock("QUARANTINE LOCKDOWN");
            }
            for (int i = 0; i < spawnPoints.Length && i < squad.Length; i++)
            {
                var enemy = Instantiate(squad[i], spawnPoints[i].position, spawnPoints[i].rotation);
                if (enemy.TryGetComponent<EnemyAI>(out var ai)) ai.AlertOnSpawn();
                alive.Add(enemy);
            }
            if (HUDController.Instance != null) HUDController.Instance.SetObjective("Survive the quarantine lockdown");
        }

        void Update()
        {
            if (!Running) return;

            timeLeft -= Time.deltaTime;
            alive.RemoveAll(e => e == null || (e.TryGetComponent<Health>(out var h) && h.IsDead));

            if (HUDController.Instance != null)
                HUDController.Instance.SetEventText($"LOCKDOWN  {Mathf.CeilToInt(Mathf.Max(0f, timeLeft))}s");

            if (timeLeft <= 0f || alive.Count == 0) End();
        }

        void End()
        {
            Running = false;
            foreach (var door in doors) door.Unlock();
            if (HUDController.Instance != null) HUDController.Instance.SetEventText(null);
            ObjectiveManager.RefreshNow();
        }
    }
}
