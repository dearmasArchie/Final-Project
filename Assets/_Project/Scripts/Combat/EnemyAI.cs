using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SignalLost
{
    /// <summary>
    /// Shared enemy brain: Patrol (blue) → Chase (yellow) → Attack (red), plus Stunned (flicker) and Dead.
    /// Sees with a vision cone + line of sight, hears Arc Pistol shots, and alerts nearby squadmates.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State { Patrol, Chase, Attack, Stunned, Dead }

        static readonly List<EnemyAI> all = new List<EnemyAI>();
        public static IReadOnlyList<EnemyAI> All => all;
        static readonly int SpeedParam = Animator.StringToHash("Speed");
        static readonly int ShootParam = Animator.StringToHash("Shoot");
        static readonly int HitParam = Animator.StringToHash("Hit");
        static readonly int DieParam = Animator.StringToHash("Die");

        [SerializeField] EnemyStats stats;
        [Tooltip("Vision origin and sensor light: the drone's eye or the robot's visor.")]
        [SerializeField] Transform eye;
        [SerializeField] Renderer sensorRenderer;
        [SerializeField] LayerMask sightMask = ~0;
        [SerializeField] Transform[] waypoints;
        [Tooltip("Set for placed enemies so they stay dead after leaving and returning to the scene. Leave empty for spawned ones.")]
        [SerializeField] string persistentId;
        [SerializeField] float corpseLifetime = 4f;

        [Header("Sensor colors")]
        [SerializeField] Color patrolColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] Color chaseColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] Color attackColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] float sensorIntensity = 4f;

        NavMeshAgent agent;
        Health health;
        EnemyAttack attack;
        Animator animator;
        Transform player;
        MaterialPropertyBlock block;
        MaterialPropertyBlock hitBlock;
        Renderer[] bodyRenderers;
        float hitFlashTimer;

        Vector3 lastKnownPosition;
        float lostTimer;
        float stunTimer;
        float flashTimer;
        float sideStepTimer;
        int waypointIndex;
        float zigZagSeed;

        public State CurrentState { get; private set; } = State.Patrol;
        public EnemyStats Stats => stats;

        void Awake()
        {
            if (!TryGetComponent<EnemyAudio>(out _)) gameObject.AddComponent<EnemyAudio>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            attack = GetComponent<EnemyAttack>();
            animator = GetComponentInChildren<Animator>();
            block = new MaterialPropertyBlock();
            hitBlock = new MaterialPropertyBlock();
            hitBlock.SetColor("_BaseColor", Color.white);
            hitBlock.SetColor("_EmissionColor", Color.white * 1.5f);
            // Mesh + skinned renderers (the robot models are skinned); not lines or particles
            var renderers = new System.Collections.Generic.List<Renderer>(GetComponentsInChildren<MeshRenderer>());
            renderers.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());
            renderers.Remove(sensorRenderer);
            bodyRenderers = renderers.ToArray();
            zigZagSeed = Random.value * 10f;
        }

        void OnEnable()
        {
            all.Add(this);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            ArcPistol.Fired += OnGunshot;
            foreach (var weakPoint in GetComponentsInChildren<WeakPoint>()) weakPoint.Crit += OnCrit;
        }

        void OnDisable()
        {
            all.Remove(this);
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            ArcPistol.Fired -= OnGunshot;
            foreach (var weakPoint in GetComponentsInChildren<WeakPoint>()) weakPoint.Crit -= OnCrit;
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(persistentId) && GameManager.Instance.IsCleared(persistentId))
            {
                Destroy(gameObject);
                return;
            }
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
            EnterState(State.Patrol);
            if (alertOnStart) Alert(broadcast: false);
        }

        bool alertOnStart;

        /// <summary>For spawned enemies (alarms, lockdown, waves): they arrive already hunting the player.</summary>
        public void AlertOnSpawn() => alertOnStart = true;

        void Update()
        {
            if (CurrentState == State.Dead || player == null) return;

            switch (CurrentState)
            {
                case State.Patrol: UpdatePatrol(); break;
                case State.Chase: UpdateChase(); break;
                case State.Attack: UpdateAttack(); break;
                case State.Stunned: UpdateStunned(); break;
            }

            if (animator != null) animator.SetFloat(SpeedParam, agent.velocity.magnitude);
            UpdateSensorColor();
            UpdateHitFlash();
        }

        // ------------------------------------------------------------------ states

        void UpdatePatrol()
        {
            if (CanSeePlayer())
            {
                Alert(broadcast: true);
                return;
            }
            if (waypoints == null || waypoints.Length == 0 || agent.pathPending) return;
            if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[waypointIndex].position);
            }
        }

        void UpdateChase()
        {
            if (CanSeePlayer())
            {
                lastKnownPosition = player.position;
                lostTimer = 0f;
                if (DistanceToPlayer() <= stats.attackRange)
                {
                    EnterState(State.Attack);
                    return;
                }
            }
            else
            {
                lostTimer += Time.deltaTime;
                if (lostTimer >= stats.loseTargetTime)
                {
                    EnterState(State.Patrol);
                    return;
                }
            }
            agent.SetDestination(WithZigZag(lastKnownPosition));
        }

        void UpdateAttack()
        {
            bool sees = CanSeePlayer();
            if (!sees || DistanceToPlayer() > stats.attackRange * 1.2f)
            {
                EnterState(State.Chase);
                return;
            }
            lastKnownPosition = player.position;

            if (sideStepTimer > 0f) sideStepTimer -= Time.deltaTime;
            else if (stats.advanceWhileAttacking && DistanceToPlayer() > stats.preferredAttackDistance)
                agent.SetDestination(WithZigZag(player.position));
            else
                agent.ResetPath();

            FaceTowards(player.position);
            attack?.Tick(this, PlayerCenter());
            if (animator != null) animator.SetBool(ShootParam, true);
        }

        void UpdateStunned()
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) EnterState(State.Chase);
        }

        void EnterState(State next)
        {
            if (CurrentState == State.Attack && next != State.Attack)
            {
                attack?.Stop();
                if (animator != null) animator.SetBool(ShootParam, false);
            }

            if (CurrentState == State.Patrol && next == State.Chase) Sfx.Play("alert_beep", transform.position);
            CurrentState = next;
            agent.isStopped = next == State.Stunned;
            agent.updateRotation = next != State.Attack;
            agent.speed = next == State.Patrol ? stats.patrolSpeed : stats.chaseSpeed;

            if (next == State.Patrol && waypoints != null && waypoints.Length > 0)
                agent.SetDestination(waypoints[waypointIndex].position);
        }

        // ------------------------------------------------------------------ reactions

        /// <summary>Switch to Chase toward the player. broadcast = also alert squadmates within range.</summary>
        public void Alert(bool broadcast)
        {
            if (CurrentState == State.Dead || player == null) return;
            lastKnownPosition = player.position;
            lostTimer = 0f;
            if (CurrentState == State.Patrol) EnterState(State.Chase);

            if (!broadcast) return;
            foreach (var other in all)
                if (other != this && Vector3.Distance(other.transform.position, transform.position) <= stats.squadAlertRadius)
                    other.Alert(broadcast: false);
        }

        void OnGunshot(Vector3 position)
        {
            if (CurrentState == State.Patrol && Vector3.Distance(position, transform.position) <= stats.hearingRange)
                Alert(broadcast: true);
        }

        void OnDamaged(float amount, DamageType type, Vector3 point)
        {
            if (CurrentState == State.Dead) return;
            if (animator != null) animator.SetTrigger(HitParam);
            Sfx.Play("enemy_hit", transform.position + Vector3.up);
            hitFlashTimer = 0.08f;
            foreach (var r in bodyRenderers) r.SetPropertyBlock(hitBlock);
            Alert(broadcast: true);

            if (CurrentState == State.Attack && stats.sideStepWhenHit && sideStepTimer <= 0f)
            {
                Vector3 side = transform.right * (Random.value < 0.5f ? -3f : 3f);
                if (NavMesh.SamplePosition(transform.position + side, out var hit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    sideStepTimer = 0.8f;
                }
            }
        }

        void OnCrit()
        {
            if (CurrentState == State.Dead) return;
            EnterState(State.Stunned);
            Sfx.Play("stun", transform.position + Vector3.up);
            stunTimer = stats.stunDuration;
        }

        void OnDied()
        {
            EnterState(State.Dead);
            Sfx.Play("enemy_die", transform.position + Vector3.up);
            agent.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            if (animator != null) animator.SetTrigger(DieParam);
            else transform.rotation *= Quaternion.Euler(80f, 0f, 0f); // greybox "falls over"

            SetSensor(Color.black);
            GameManager.Instance.RegisterEnemyDestroyed();
            if (!string.IsNullOrEmpty(persistentId)) GameManager.Instance.MarkCleared(persistentId);
            Destroy(gameObject, corpseLifetime);
        }

        // ------------------------------------------------------------------ helpers

        bool CanSeePlayer()
        {
            Vector3 toPlayer = PlayerCenter() - eye.position;
            float distance = toPlayer.magnitude;
            if (distance > stats.visionRange) return false;

            // Always notice a player who is very close, even behind.
            if (distance > 2.5f && Vector3.Angle(eye.forward, toPlayer) > stats.visionAngle / 2f) return false;

            return Physics.Raycast(eye.position, toPlayer, out var hit, distance + 0.5f, sightMask, QueryTriggerInteraction.Ignore)
                   && hit.collider.GetComponentInParent<PlayerHealth>() != null;
        }

        float DistanceToPlayer() => Vector3.Distance(transform.position, player.position);

        Vector3 PlayerCenter() => player.position + Vector3.up * 1.2f;

        Vector3 WithZigZag(Vector3 destination)
        {
            if (stats.zigZagAmplitude <= 0f) return destination;
            Vector3 toTarget = destination - transform.position;
            Vector3 side = Vector3.Cross(Vector3.up, toTarget.normalized);
            float wave = Mathf.Sin((Time.time + zigZagSeed) * stats.zigZagFrequency);
            return destination + side * wave * stats.zigZagAmplitude;
        }

        void FaceTowards(Vector3 position)
        {
            Vector3 flat = position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 360f * Time.deltaTime);
        }

        void UpdateHitFlash()
        {
            if (hitFlashTimer <= 0f) return;
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f) foreach (var r in bodyRenderers) r.SetPropertyBlock(null);
        }

        /// <summary>Attack telegraph: the sensor flashes white for this long.</summary>
        public void FlashSensor(float duration) => flashTimer = duration;

        void UpdateSensorColor()
        {
            Color color;
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                color = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? Color.white : attackColor;
            }
            else switch (CurrentState)
            {
                case State.Chase: color = chaseColor; break;
                case State.Attack: color = attackColor; break;
                case State.Stunned: color = Mathf.Repeat(Time.time * 15f, 1f) < 0.5f ? attackColor : Color.black; break;
                default: color = patrolColor; break;
            }
            SetSensor(color);
        }

        void SetSensor(Color color)
        {
            if (sensorRenderer == null) return;
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", color * sensorIntensity);
            sensorRenderer.SetPropertyBlock(block);
        }

        void OnDrawGizmosSelected()
        {
            if (stats == null || eye == null) return;
            Gizmos.color = Color.yellow;
            Vector3 left = Quaternion.Euler(0, -stats.visionAngle / 2f, 0) * eye.forward;
            Vector3 right = Quaternion.Euler(0, stats.visionAngle / 2f, 0) * eye.forward;
            Gizmos.DrawRay(eye.position, left * stats.visionRange);
            Gizmos.DrawRay(eye.position, right * stats.visionRange);
        }
    }
}
