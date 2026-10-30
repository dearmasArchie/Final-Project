using System;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Put on the collider of a glowing weak point (drone eye, robot visor, boss vent).
    /// Multiplies Pulse hits and passes them to the Health on a parent. Pulse 20 x 2.25 = 45 crit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeakPoint : MonoBehaviour, IDamageable
    {
        [SerializeField] float multiplier = 2.25f;

        IDamageable owner;

        /// <summary>Raised on every crit; enemy AI listens to start the 0.5 s stun.</summary>
        public event Action Crit;

        void Awake()
        {
            // Skip this component and find the IDamageable above it.
            owner = transform.parent != null ? transform.parent.GetComponentInParent<IDamageable>() : null;
        }

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (owner == null) return;
            // Only precise Pulse shots crit; beams and explosions pass through at normal damage.
            if (type != DamageType.Pulse)
            {
                owner.TakeDamage(amount, type, hitPoint);
                return;
            }
            owner.TakeDamage(amount * multiplier, type, hitPoint);
            Crit?.Invoke();
        }
    }
}
