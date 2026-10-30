using System;
using UnityEngine;

namespace SignalLost
{
    /// <summary>The player's HP. Enemies and hazards damage it through IDamageable.</summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsDead { get; private set; }

        /// <summary>Damage amount and the world point it came from (for the damage-direction indicator).</summary>
        public event Action<float, Vector3> Damaged;
        public event Action Died;

        void Awake() => Current = maxHealth;

        void Start() => RefreshHud();

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            RefreshHud();
            if (HUDController.Instance != null) HUDController.Instance.FlashDamage(amount);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(Mathf.Clamp(amount / 400f, 0.015f, 0.08f), 0.2f);
            Damaged?.Invoke(amount, hitPoint);

            if (Current <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            RefreshHud();
        }

        /// <summary>Used when loading saved state (scene change or checkpoint).</summary>
        public void SetHealth(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxHealth);
            IsDead = Current <= 0f;
            RefreshHud();
        }

        void RefreshHud()
        {
            if (HUDController.Instance != null) HUDController.Instance.SetHealth(Current, maxHealth);
        }
    }
}
