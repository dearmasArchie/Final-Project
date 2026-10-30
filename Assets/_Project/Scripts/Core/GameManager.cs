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

        // ---- Armory code puzzle (random per run) ----

        /// <summary>
        /// The maintenance bunker puzzle: four colored lockers each show one digit, the wall gives the color order,
        /// and one locker's digit is only visible in the dark.
        /// </summary>
        public class ArmoryPuzzleData
        {
            public static readonly string[] ColorNames = { "BLUE", "RED", "GREEN", "YELLOW" };
            public int[] digitByColor;   // digit stenciled on each colored locker
            public int[] colorOrder;     // color indices in the order the code is read
            public int hiddenColor;      // locker whose digit is glow paint (lights off only)
            public int[] decoys;         // misleading numbers painted around the bunker
            public string Code
            {
                get
                {
                    var code = "";
                    foreach (int color in colorOrder) code += digitByColor[color];
                    return code;
                }
            }
        }

        ArmoryPuzzleData armoryPuzzle;

        public ArmoryPuzzleData ArmoryPuzzle
        {
            get
            {
                if (armoryPuzzle != null) return armoryPuzzle;
                var order = new[] { 0, 1, 2, 3 };
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                armoryPuzzle = new ArmoryPuzzleData
                {
                    digitByColor = new[] { Digit(), Digit(), Digit(), Digit() },
                    colorOrder = order,
                    hiddenColor = UnityEngine.Random.Range(0, 4),
                    decoys = new[] { UnityEngine.Random.Range(2, 10), UnityEngine.Random.Range(1, 10), UnityEngine.Random.Range(10, 40) }
                };
                return armoryPuzzle;
            }
        }

        static int Digit() => UnityEngine.Random.Range(0, 10);

        int[] gaugeReadings;

        /// <summary>Wing A gauge lock: the three gauge needles' readings (0–9), random per run. The code is them in order.</summary>
        public int[] GaugeReadings => gaugeReadings ??= new[] { Digit(), Digit(), Digit() };

        // ---- World flags: cleared encounters, opened doors, collected pickups ----
        readonly HashSet<string> clearedIds = new HashSet<string>();

        // ---- Stats for the Victory screen ----
        public int EnemiesDestroyed { get; private set; }
        public int ShotsFired { get; private set; }
        public int ShotsHit { get; private set; }
        public float Accuracy => ShotsFired == 0 ? 0f : (float)ShotsHit / ShotsFired;
        public float ElapsedTime => PurgeDuration - TimeRemaining;

        /// <summary>True after victory; the timer stops and can't end the run any more.</summary>
        public bool RunOver { get; private set; }

        // ---- Checkpoint snapshot ----
        class Snapshot
        {
            public bool[] fuses;
            public HashSet<string> cleared;
            public float timeRemaining, health;
            public int magazine, reserve, enemiesDestroyed, shotsFired, shotsHit;
            public bool hasPistol;
            public string scene, spawnId;
        }

        Snapshot checkpoint;
        public bool HasCheckpoint => checkpoint != null;
        /// <summary>Identifies the last save (checkpoint + progress), so a checkpoint doesn't save twice for the same progress.</summary>
        public string LastCheckpointKey { get; private set; }

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
            if (scene.buildIndex > 0 && !TimerRunning && !RunOver && TimeRemaining >= PurgeDuration)
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

        // ---- Checkpoints ----

        public void SaveCheckpoint(string key, string scene, string spawnId, float health, int magazine, int reserve)
        {
            LastCheckpointKey = key;
            checkpoint = new Snapshot
            {
                fuses = (bool[])fuses.Clone(),
                cleared = new HashSet<string>(clearedIds),
                timeRemaining = TimeRemaining,
                health = health,
                magazine = magazine,
                reserve = reserve,
                enemiesDestroyed = EnemiesDestroyed,
                shotsFired = ShotsFired,
                shotsHit = ShotsHit,
                hasPistol = HasArcPistol,
                scene = scene,
                spawnId = spawnId
            };
        }

        /// <summary>Puts the run back exactly as it was at the last checkpoint and reloads that scene.</summary>
        public void RestoreCheckpoint()
        {
            var c = checkpoint;
            Array.Copy(c.fuses, fuses, fuses.Length);
            FusesCollected = 0;
            foreach (var f in fuses) if (f) FusesCollected++;
            clearedIds.Clear();
            clearedIds.UnionWith(c.cleared);
            TimeRemaining = c.timeRemaining;
            SavePlayerState(c.health, c.magazine, c.reserve);
            HasArcPistol = c.hasPistol;
            EnemiesDestroyed = c.enemiesDestroyed;
            ShotsFired = c.shotsFired;
            ShotsHit = c.shotsHit;
            NextSpawnPointId = c.spawnId;
            RunOver = false;
            TimerRunning = TimeRemaining > 0f;

            Time.timeScale = 1f;
            SceneLoader.Instance.Load(c.scene, savePlayer: false);
        }

        /// <summary>Victory: stop the clock.</summary>
        public void EndRun()
        {
            RunOver = true;
            TimerRunning = false;
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
            RunOver = false;
            armoryPuzzle = null;
            gaugeReadings = null;
            checkpoint = null;
            LastCheckpointKey = null;
            FusesChanged?.Invoke(0);
            EnemiesDestroyedChanged?.Invoke(0);
        }
    }
}
