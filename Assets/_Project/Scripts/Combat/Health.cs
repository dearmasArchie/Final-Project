using System;
using UnityEngine;

namespace SignalLost
{
    /// <summary>Generic HP for enemies, the boss, pylons, crates, and test dummies.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] bool destroyOnDeath = true;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsDead { get; private set; }

        /// <summary>Damage dealt (after any multipliers), its type, and where it hit.</summary>
        public event Action<float, DamageType, Vector3> Damaged;
        public event Action Died;

        void Awake() => Current = maxHealth;

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount, type, hitPoint);

            if (Current > 0f) return;
            IsDead = true;
            Died?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            IsDead = false;
        }
    }
}
