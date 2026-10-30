using UnityEngine;

namespace SignalLost
{
    /// <summary>Titan chest core: takes double damage, but only while the boss is stunned in Phase 2.</summary>
    [RequireComponent(typeof(Collider))]
    public class BossCore : MonoBehaviour, IDamageable
    {
        [SerializeField] BossController boss;
        [SerializeField] float multiplier = 2f;

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (boss.CoreExposed) boss.Health.TakeDamage(amount * multiplier, type, hitPoint);
        }
    }
}
