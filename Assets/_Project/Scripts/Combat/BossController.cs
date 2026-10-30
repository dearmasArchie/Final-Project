using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace SignalLost
{
    /// <summary>
    /// Unit 00-Titan, Phase 1 ("Riot Shield"): advances slowly behind its shield and fires shootable
    /// plasma orbs every few seconds. An Overload Beam drops the shield and exposes the two shoulder vents,
    /// the only place it takes damage. Phase 2 (pylon tethers) is added in Phase 4 of the roadmap.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class BossController : MonoBehaviour
    {
        public const string DefeatedId = "BossDefeated";

        [SerializeField] EnergyShield riotShield;
        [SerializeField] Transform orbLauncher;
        [SerializeField] BossOrb orbPrefab;
        [SerializeField] int orbsPerVolley = 3;
        [SerializeField] float volleyInterval = 5f;
        [SerializeField] float keepDistance = 9f;
        [SerializeField] string displayName = "UNIT 00-TITAN";

        public UnityEvent onDefeated = new UnityEvent();

        NavMeshAgent agent;
        Transform player;
        float volleyTimer = 3f;

        public Health Health { get; private set; }
        public bool VentsExposed => !riotShield.IsUp;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            Health = GetComponent<Health>();
        }

        void OnEnable()
        {
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        void OnDisable()
        {
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
            if (HUDController.Instance != null) HUDController.Instance.HideEventBar();
        }

        void Start()
        {
            if (GameManager.Instance.IsCleared(DefeatedId))
            {
                onDefeated.Invoke();
                Destroy(gameObject);
                return;
            }
            player = GameObject.FindGameObjectWithTag("Player").transform;
            UpdateBar();
        }

        void Update()
        {
            if (Health.IsDead || player == null) return;

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > keepDistance) agent.SetDestination(player.position);
            else agent.ResetPath();

            Vector3 flat = player.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 45f * Time.deltaTime);

            volleyTimer -= Time.deltaTime;
            if (volleyTimer <= 0f)
            {
                volleyTimer = volleyInterval;
                for (int i = 0; i < orbsPerVolley; i++)
                {
                    float spread = (i - (orbsPerVolley - 1) / 2f) * 25f;
                    Instantiate(orbPrefab, orbLauncher.position, orbLauncher.rotation * Quaternion.Euler(-20f, spread, 0f));
                }
            }
        }

        void OnDamaged(float amount, DamageType type, Vector3 point) => UpdateBar();

        void OnDied()
        {
            agent.enabled = false;
            GameManager.Instance.MarkCleared(DefeatedId);
            GameManager.Instance.RegisterEnemyDestroyed();
            if (HUDController.Instance != null) HUDController.Instance.HideEventBar();
            ExplosionFlash.Spawn(transform.position + Vector3.up * 2.5f, 6f);
            onDefeated.Invoke();
            ObjectiveManager.RefreshNow();
            Destroy(gameObject, 0.2f);
        }

        void UpdateBar()
        {
            if (HUDController.Instance != null)
                HUDController.Instance.SetEventBar(displayName, Health.Current / Health.Max);
        }
    }
}
