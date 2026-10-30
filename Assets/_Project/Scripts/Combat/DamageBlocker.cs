using UnityEngine;

namespace SignalLost
{
    /// <summary>Armor: absorbs hits on this collider so they never reach the Health above it (the Titan's body).</summary>
    public class DamageBlocker : MonoBehaviour, IDamageable
    {
        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint) { }
    }
}
