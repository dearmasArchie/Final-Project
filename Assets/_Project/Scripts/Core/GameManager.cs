using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalLost
{
    /// <summary>
    /// Run state that must survive scene loads: fuses, purge timer, player HP/ammo, cleared encounters, stats.
    /// Created automatically before the first scene loads, so any scene can be played directly from the Editor.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const int FuseCount = 3;
        public const float PurgeDuration = 20f * 60f;

        public static GameManager Instance { get; private set; }

        // ---- Events (subscribe in OnEnable, unsubscribe in OnDisable) ----
        public event Action<int> FusesChanged;          // new collected count
        public event Action TimeUp;                     // purge timer reached 0
        public event Action<int> EnemiesDestroyedChanged;

        // ---- Fuses ----
        readonly bool[] fuses = new bool[FuseCount];
        public int FusesCollected { get; private set; }

        // ---- Purge timer ----
        public float TimeRemaining { get; private set; } = PurgeDuration;
        public bool TimerRunning { get; private set; }

        // ---- Player state carried between scenes ----
        public bool HasPlayerState { get; private set; }
        public float PlayerHealth { get; private set; }
        public int PlayerMagazine { get; private set; }
        public int PlayerReserveAmmo { get; private set; }
        public bool HasArcPistol { get; set; }

        /// <summary>ID of the SpawnPoint the player should appear at after the next scene load.</summary>
        public string NextSpawnPointId { get; set; }

        // ---- World flags: cleared encounters, opened doors, collected pickups ----
        readonly HashSet<string> clearedIds = new HashSet<string>();

        // ---- Stats for the Victory screen ----
        public int EnemiesDestroyed { get; private set; }
        public int ShotsFired { get; private set; }
        public int ShotsHit { get; private set; }
        public float Accuracy => ShotsFired == 0 ? 0f : (float)ShotsHit / ShotsFired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject(nameof(GameManager));
            go.AddComponent<GameManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // The purge countdown starts the first time a gameplay scene loads (build index 0 is the main menu).
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex > 0 && !TimerRunning && TimeRemaining >= PurgeDuration)
                StartTimer();
        }

        void Update()
        {
            if (!TimerRunning) return;

            // Time.deltaTime is 0 while paused (timeScale = 0), so the timer stops in the pause menu.
            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (TimeRemaining <= 0f)
            {
                TimerRunning = false;
                TimeUp?.Invoke();
            }
        }

        // ---- Fuses ----

        /// <summary>Fuse numbers are 1, 2 and 3.</summary>
        public bool HasFuse(int fuseNumber) => fuses[fuseNumber - 1];

        public void CollectFuse(int fuseNumber)
        {
            if (HasFuse(fuseNumber)) return;
            fuses[fuseNumber - 1] = true;
            FusesCollected++;
            FusesChanged?.Invoke(FusesCollected);
        }

        // ---- Timer ----

        public void StartTimer() => TimerRunning = TimeRemaining > 0f;
        public void StopTimer() => TimerRunning = false;

        // ---- Player state ----

        /// <summary>Call before leaving a scene so HP and ammo carry over.</summary>
        public void SavePlayerState(float health, int magazine, int reserveAmmo)
        {
            PlayerHealth = health;
            PlayerMagazine = magazine;
            PlayerReserveAmmo = reserveAmmo;
            HasPlayerState = true;
        }

        // ---- World flags ----

        public void MarkCleared(string id) => clearedIds.Add(id);
        public bool IsCleared(string id) => clearedIds.Contains(id);

        // ---- Stats ----

        public void RegisterEnemyDestroyed()
        {
            EnemiesDestroyed++;
            EnemiesDestroyedChanged?.Invoke(EnemiesDestroyed);
        }

        public void RegisterShot(bool hit)
        {
            ShotsFired++;
            if (hit) ShotsHit++;
        }

        // ---- Run lifecycle ----

        /// <summary>Clears everything for a fresh run (Play from the main menu, or Restart Game).</summary>
        public void ResetRun()
        {
            Array.Clear(fuses, 0, fuses.Length);
            FusesCollected = 0;
            TimeRemaining = PurgeDuration;
            TimerRunning = false;
            HasPlayerState = false;
            HasArcPistol = false;
            NextSpawnPointId = null;
            clearedIds.Clear();
            EnemiesDestroyed = 0;
            ShotsFired = 0;
            ShotsHit = 0;
            FusesChanged?.Invoke(0);
            EnemiesDestroyedChanged?.Invoke(0);
        }
    }
}
