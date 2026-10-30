using System;
using UnityEngine;
using UnityEngine.Events;

namespace SignalLost
{
    /// <summary>
    /// Radio tower defense: a 90-second signal upload with timed enemy waves from the spawn gates.
    /// The upload only progresses while the player stays inside the compound.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Serializable]
        public class Wave
        {
            [Tooltip("Seconds of upload progress before this wave arrives.")]
            public float atSecond;
            public GameObject[] enemies;
        }

        public const string CompletedId = "UploadComplete";

        [SerializeField] float uploadDuration = 90f;
        [SerializeField] Transform[] gates;
        [SerializeField] Wave[] waves;
        [SerializeField] Transform compoundCenter;
        [SerializeField] float compoundRadius = 18f;

        public UnityEvent onUploadComplete = new UnityEvent();

        float progress;
        int nextWave;
        Transform player;

        public bool Running { get; private set; }

        void Start()
        {
            if (GameManager.Instance.IsCleared(CompletedId)) onUploadComplete.Invoke();
        }

        public void Begin()
        {
            if (Running || GameManager.Instance.IsCleared(CompletedId)) return;
            Running = true;
            player = GameObject.FindGameObjectWithTag("Player").transform;
            if (HUDController.Instance != null) HUDController.Instance.SetObjective("Defend the radio tower until the upload finishes");
        }

        void Update()
        {
            if (!Running) return;

            bool inside = Vector3.Distance(player.position, compoundCenter.position) <= compoundRadius;
            if (inside) progress += Time.deltaTime;

            while (nextWave < waves.Length && progress >= waves[nextWave].atSecond)
                SpawnWave(waves[nextWave++]);

            float percent = Mathf.Clamp01(progress / uploadDuration);
            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetEventBar($"SIGNAL UPLOAD  {Mathf.FloorToInt(percent * 100f)}%", percent);
                HUDController.Instance.SetEventText(inside ? null : "UPLOAD PAUSED – RETURN TO THE TOWER");
            }

            if (progress >= uploadDuration) Complete();
        }

        void SpawnWave(Wave wave)
        {
            for (int i = 0; i < wave.enemies.Length; i++)
            {
                var gate = gates[i % gates.Length];
                var enemy = Instantiate(wave.enemies[i], gate.position + gate.right * (i / gates.Length) * 1.5f, gate.rotation);
                if (enemy.TryGetComponent<EnemyAI>(out var ai)) ai.AlertOnSpawn();
            }
        }

        void Complete()
        {
            Running = false;
            GameManager.Instance.MarkCleared(CompletedId);
            if (HUDController.Instance != null)
            {
                HUDController.Instance.HideEventBar();
                HUDController.Instance.SetEventText(null);
            }
            onUploadComplete.Invoke();
            ObjectiveManager.RefreshNow();
        }
    }
}
