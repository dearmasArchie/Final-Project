using UnityEngine;

namespace SignalLost
{
    /// <summary>Recon Stinger's close-range shock beam: continuous damage while the player is in range and in sight.</summary>
    public class ShockBeamAttack : EnemyAttack
    {
        [SerializeField] Transform emitter;
        [SerializeField] LineRenderer beam;
        [SerializeField] LayerMask sightMask = ~0;
        [SerializeField] float range = 4f;
        [SerializeField] float damagePerSecond = 12f;
        [SerializeField] float damageTickInterval = 0.25f;

        float tickTimer;

        void Awake() => SetBeam(false, Vector3.zero);

        public override void Tick(EnemyAI ai, Vector3 target)
        {
            Vector3 from = emitter.position;
            PlayerHealth victim = null;
            if (Vector3.Distance(from, target) <= range
                && Physics.Raycast(from, target - from, out var hit, range, sightMask, QueryTriggerInteraction.Ignore))
                victim = hit.collider.GetComponentInParent<PlayerHealth>();

            SetBeam(victim != null, target);
            if (victim == null) return;

            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = damageTickInterval;
            victim.TakeDamage(damagePerSecond * damageTickInterval, DamageType.Hazard, from);
        }

        public override void Stop() => SetBeam(false, Vector3.zero);

        void SetBeam(bool on, Vector3 target)
        {
            if (beam == null) return;
            beam.enabled = on;
            if (!on) return;
            beam.SetPosition(0, emitter.position);
            beam.SetPosition(1, target);
        }
    }
}
