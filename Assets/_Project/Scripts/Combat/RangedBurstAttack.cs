using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Enforcer Unit's arm laser: the visor flashes for the telegraph time, then fires a burst of visible bolts.
    /// Defaults: 3 bolts x 6 damage every 2 s.
    /// </summary>
    public class RangedBurstAttack : EnemyAttack
    {
        [SerializeField] Transform muzzle;
        [SerializeField] Projectile boltPrefab;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] int boltsPerBurst = 3;
        [SerializeField] float damagePerBolt = 6f;
        [SerializeField] float timeBetweenBursts = 2f;
        [SerializeField] float timeBetweenBolts = 0.12f;
        [SerializeField] float telegraphTime = 0.4f;
        [SerializeField] float spreadDegrees = 2.5f;

        float cooldown = 1f;
        float telegraphTimer = -1f;
        int boltsLeft;
        float boltTimer;

        public override void Tick(EnemyAI ai, Vector3 target)
        {
            if (boltsLeft > 0)
            {
                boltTimer -= Time.deltaTime;
                if (boltTimer <= 0f)
                {
                    FireBolt(target);
                    boltsLeft--;
                    boltTimer = timeBetweenBolts;
                }
                return;
            }

            if (telegraphTimer >= 0f)
            {
                telegraphTimer -= Time.deltaTime;
                if (telegraphTimer < 0f)
                {
                    boltsLeft = boltsPerBurst;
                    boltTimer = 0f;
                    cooldown = timeBetweenBursts;
                }
                return;
            }

            cooldown -= Time.deltaTime;
            if (cooldown <= 0f)
            {
                telegraphTimer = telegraphTime;
                ai.FlashSensor(telegraphTime);
            }
        }

        public override void Stop()
        {
            telegraphTimer = -1f;
            boltsLeft = 0;
            cooldown = Mathf.Max(cooldown, 0.5f);
        }

        void FireBolt(Vector3 target)
        {
            var direction = Quaternion.Euler(Random.Range(-spreadDegrees, spreadDegrees), Random.Range(-spreadDegrees, spreadDegrees), 0f)
                            * (target - muzzle.position).normalized;
            var bolt = Instantiate(boltPrefab, muzzle.position, Quaternion.LookRotation(direction));
            bolt.Launch(damagePerBolt, DamageType.Hazard, hitMask, countForAccuracy: false);
            Sfx.Play("enemy_laser", muzzle.position);
        }
    }
}
