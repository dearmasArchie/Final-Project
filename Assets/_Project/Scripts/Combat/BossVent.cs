using UnityEngine;

namespace SignalLost
{
    /// <summary>Titan shoulder vent: takes double damage, but only while the riot shield is down.</summary>
    [RequireComponent(typeof(Collider))]
    public class BossVent : MonoBehaviour, IDamageable
    {
        [SerializeField] BossController boss;
        [SerializeField] float multiplier = 2f;

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (boss.VentsExposed) boss.Health.TakeDamage(amount * multiplier, type, hitPoint);
        }
    }
}
