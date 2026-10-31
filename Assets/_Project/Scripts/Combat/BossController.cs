using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace SignalLost
{
    /// <summary>
    /// Unit 00-Titan.
    /// Phase 1 "Riot Shield" (500 → 250 HP): advances behind its shield and fires shootable plasma orbs.
    /// An Overload Beam drops the shield and exposes the two shoulder vents, the only place it takes damage.
    /// Phase 2 "Pylon Tether" (250 → 0 HP): stops at the pier, tethers to two power pylons and becomes invulnerable,
    /// stomps out shockwave rings to jump over, and calls in two Recon Stingers. Destroying both pylons stuns it
    /// and exposes the chest core (double damage); when the stun ends the pylons reboot at half health.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class BossController : MonoBehaviour
    {
        public const string DefeatedId = "BossDefeated";

        [Header("Phase 1")]
        [SerializeField] EnergyShield riotShield;
        [SerializeField] Transform orbLauncher;
        [SerializeField] BossOrb orbPrefab;
        [SerializeField] int orbsPerVolley = 3;
        [SerializeField] float volleyInterval = 5f;
        [SerializeField] float keepDistance = 9f;

        [Header("Phase 2")]
        [SerializeField, Tooltip("Half of max HP: crossing it starts Phase 2")] float phase2Threshold = 500f;
        [SerializeField, Tooltip("The Titan repairs itself to full HP when Phase 2 begins")] bool regenerateInPhase2 = true;
        [SerializeField] BossPylon[] pylons;
        [SerializeField] LightningLine[] tethers;
        [SerializeField] Transform tetherAnchor;
        [SerializeField] Renderer coreRenderer;
        [SerializeField] ShockwaveRing shockwavePrefab;
        [SerializeField] float stompInterval = 6f;
        [SerializeField] float stunDuration = 5f;
        [SerializeField] float pylonRebootFraction = 0.5f;
        [SerializeField] GameObject reinforcementPrefab;
        [SerializeField] Transform[] reinforcementPoints;

        [SerializeField] string displayName = "UNIT 00-TITAN";

        public UnityEvent onDefeated = new UnityEvent();

        NavMeshAgent agent;
        Transform player;
        float volleyTimer = 3f;
        float stompTimer;
        float stunTimer;
        MaterialPropertyBlock coreBlock;

        public Health Health { get; private set; }
        public bool InPhase2 { get; private set; }
        public bool Stunned => stunTimer > 0f;
        public bool VentsExposed => !InPhase2 && !riotShield.IsUp;
        public bool CoreExposed => InPhase2 && Stunned;

        bool stompCharging;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            Health = GetComponent<Health>();
            coreBlock = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            foreach (var pylon in pylons) pylon.Destroyed += OnPylonDestroyed;
        }

        void OnDisable()
        {
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
            foreach (var pylon in pylons) pylon.Destroyed -= OnPylonDestroyed;
            if (HUDController.Instance != null) HUDController.Instance.HideEventBar();
        }

        void Start()
        {
            if (GameManager.Instance.IsCleared(DefeatedId))
            {
                onDefeated.Invoke();
                foreach (var pylon in pylons) pylon.gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            player = GameObject.FindGameObjectWithTag("Player").transform;
            foreach (var tether in tethers) tether.Hide();
            SetCoreGlow(false);
            UpdateBar();
        }

        void Update()
        {
            if (Health.IsDead || player == null) return;

            if (InPhase2) UpdatePhase2();
            else UpdatePhase1();
        }

        // ------------------------------------------------------------------ Phase 1

        void UpdatePhase1()
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > keepDistance) agent.SetDestination(player.position);
            else agent.ResetPath();
            FacePlayer(45f);
            FireOrbsWhenReady(volleyInterval);
        }

        // ------------------------------------------------------------------ Phase 2

        void EnterPhase2()
        {
            InPhase2 = true;
            if (regenerateInPhase2) Health.SetHealth(Health.Max); // its repair systems kick in at half HP
            Sfx.Play("siren", transform.position);
            agent.ResetPath();
            agent.isStopped = true;
            riotShield.gameObject.SetActive(false);
            for (int i = 0; i < tethers.Length && i < pylons.Length; i++) tethers[i].Connect(tetherAnchor, pylons[i].TetherPoint);
            stompTimer = 2f;
            foreach (var point in reinforcementPoints)
            {
                var enemy = Instantiate(reinforcementPrefab, point.position, point.rotation);
                if (enemy.TryGetComponent<EnemyAI>(out var ai)) ai.AlertOnSpawn();
            }
            Radio.Say("It's repairing itself – back to full! It's drawing power from the pylons and invulnerable while tethered. Take out both pylons, then hit the glowing core!");
            UpdateBar();
        }

        void UpdatePhase2()
        {
            if (Stunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f) EndStun();
                return;
            }

            FacePlayer(30f);
            FireOrbsWhenReady(volleyInterval + 2f);
            stompTimer -= Time.deltaTime;
            if (!stompCharging && stompTimer <= 1.6f)
            {
                stompCharging = true;
                Sfx.Play("stomp_charge", transform.position);
            }
            if (stompTimer <= 0f)
            {
                stompTimer = stompInterval;
                stompCharging = false;
                Sfx.Play("stomp", transform.position);
                Instantiate(shockwavePrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);
            }
        }

        void OnPylonDestroyed(BossPylon pylon)
        {
            int index = System.Array.IndexOf(pylons, pylon);
            if (index >= 0 && index < tethers.Length) tethers[index].Hide();
            foreach (var p in pylons) if (!p.IsDown) return;

            // Both pylons down: stunned, core exposed
            stunTimer = stunDuration;
            SetCoreGlow(true);
            if (HUDController.Instance != null) HUDController.Instance.ShowToast("CORE EXPOSED – SHOOT THE CHEST!");
            UpdateBar();
        }

        void EndStun()
        {
            stunTimer = 0f;
            SetCoreGlow(false);
            for (int i = 0; i < pylons.Length; i++)
            {
                pylons[i].Reboot(pylonRebootFraction);
                if (i < tethers.Length) tethers[i].Connect(tetherAnchor, pylons[i].TetherPoint);
            }
            if (HUDController.Instance != null) HUDController.Instance.ShowToast("PYLONS REBOOTING");
            UpdateBar();
        }

        // ------------------------------------------------------------------ shared

        void FireOrbsWhenReady(float interval)
        {
            volleyTimer -= Time.deltaTime;
            if (volleyTimer > 0f) return;
            volleyTimer = interval;
            Sfx.Play("orb_fire", orbLauncher.position);
            for (int i = 0; i < orbsPerVolley; i++)
            {
                float spread = (i - (orbsPerVolley - 1) / 2f) * 25f;
                Instantiate(orbPrefab, orbLauncher.position, orbLauncher.rotation * Quaternion.Euler(-20f, spread, 0f));
            }
        }

        void FacePlayer(float degreesPerSecond)
        {
            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), degreesPerSecond * Time.deltaTime);
        }

        void OnDamaged(float amount, DamageType type, Vector3 point)
        {
            if (!InPhase2 && Health.Current <= phase2Threshold) EnterPhase2();
            UpdateBar();
        }

        void OnDied()
        {
            agent.enabled = false;
            foreach (var tether in tethers) tether.Hide();
            GameManager.Instance.MarkCleared(DefeatedId);
            GameManager.Instance.RegisterEnemyDestroyed();
            if (HUDController.Instance != null) HUDController.Instance.HideEventBar();
            ExplosionFlash.Spawn(transform.position + Vector3.up * 2.5f, 6f);
            foreach (var pylon in pylons) pylon.gameObject.SetActive(false);
            onDefeated.Invoke();
            ObjectiveManager.RefreshNow();
            Destroy(gameObject, 0.2f);
        }

        void SetCoreGlow(bool exposed)
        {
            if (coreRenderer == null) return;
            var color = exposed ? new Color(1f, 0.85f, 0.2f) : new Color(0.25f, 0.2f, 0.1f);
            coreBlock.SetColor("_BaseColor", color);
            coreBlock.SetColor("_EmissionColor", exposed ? color * 4f : Color.black);
            coreRenderer.SetPropertyBlock(coreBlock);
        }

        void UpdateBar()
        {
            if (HUDController.Instance == null) return;
            string state = !InPhase2 ? "" : Stunned ? "  –  CORE EXPOSED" : "  –  TETHERED";
            HUDController.Instance.SetEventBar(displayName + state, Health.Current / Health.Max);
        }

        /// <summary>Automated tests only.</summary>
        public void ForcePhase2ForTest() => Health.SetHealth(phase2Threshold - 1f);
        public void CheckPhaseForTest() => OnDamaged(0f, DamageType.Hazard, transform.position);
    }
}
