using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SignalLost
{
    /// <summary>Red hazard barrel: any hit makes it explode for 75 damage within 5 m. The player takes half.</summary>
    public class ExplosiveBarrel : MonoBehaviour, IDamageable
    {
        [SerializeField] float damage = 75f;
        [SerializeField] float radius = 5f;
        [SerializeField] float playerDamageMultiplier = 0.5f;
        [Tooltip("Short delay so chained barrels go off one after another.")]
        [SerializeField] float fuseDelay = 0.1f;

        bool exploding;

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (exploding) return;
            exploding = true;
            StartCoroutine(Explode());
        }

        IEnumerator Explode()
        {
            yield return new WaitForSeconds(fuseDelay);
            Vector3 center = transform.position + Vector3.up * 0.5f;
            ExplosionFlash.Spawn(center, radius);
            ArcPistol.RaiseNoise(center);

            var damaged = new HashSet<Component>();
            foreach (var col in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                // Weak points would double-count; damage each body once through its main IDamageable.
                if (col.GetComponent<WeakPoint>() != null) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || ReferenceEquals(target, this)) continue;
                var component = (Component)target;
                if (!damaged.Add(component)) continue;

                float amount = target is PlayerHealth ? damage * playerDamageMultiplier : damage;
                target.TakeDamage(amount, DamageType.Explosion, center);
            }
            Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, radius);
        }
    }
}
