using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// A fast visible bolt. Moves by raycasting each frame so it never tunnels through thin colliders.
    /// Used for the Arc Pistol's Pulse and (with a different prefab) enemy lasers.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] float speed = 60f;
        [SerializeField] float lifetime = 2f;

        float damage;
        DamageType damageType;
        LayerMask hitMask;
        bool countForAccuracy;
        float age;

        public void Launch(float damage, DamageType type, LayerMask hitMask, bool countForAccuracy)
        {
            this.damage = damage;
            damageType = type;
            this.hitMask = hitMask;
            this.countForAccuracy = countForAccuracy;
        }

        void Update()
        {
            float step = speed * Time.deltaTime;
            if (Physics.Raycast(transform.position, transform.forward, out var hit, step, hitMask, QueryTriggerInteraction.Ignore))
            {
                OnHit(hit);
                return;
            }

            transform.position += transform.forward * step;
            age += Time.deltaTime;
            if (age >= lifetime) Finish(false);
        }

        void OnHit(RaycastHit hit)
        {
            // A WeakPoint on the hit collider takes priority over the body's Health.
            if (!hit.collider.TryGetComponent<IDamageable>(out var target))
                target = hit.collider.GetComponentInParent<IDamageable>();
            target?.TakeDamage(damage, damageType, hit.point);
            transform.position = hit.point;
            Finish(target != null);
        }

        void Finish(bool hitDamageable)
        {
            if (countForAccuracy && GameManager.Instance != null) GameManager.Instance.RegisterShot(hitDamageable);
            Destroy(gameObject);
        }
    }
}
