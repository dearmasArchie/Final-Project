using UnityEngine;

namespace SignalLost
{
    /// <summary>Titan plasma orb: drifts slowly toward the player, hurts on contact, and pops from any hit.</summary>
    public class BossOrb : MonoBehaviour, IDamageable
    {
        [SerializeField] float speed = 6f;
        [SerializeField] float turnRate = 60f;
        [SerializeField] float damage = 15f;
        [SerializeField] float lifetime = 8f;
        [SerializeField] float hitRadius = 0.8f;

        Transform player;

        void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (player == null) return;
            Vector3 target = player.position + Vector3.up * 1.2f;
            Vector3 toTarget = target - transform.position;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toTarget), turnRate * Time.deltaTime);
            transform.position += transform.forward * speed * Time.deltaTime;

            if (toTarget.magnitude <= hitRadius)
            {
                player.GetComponent<PlayerHealth>().TakeDamage(damage, DamageType.Hazard, transform.position);
                Destroy(gameObject);
            }
        }

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint) => Destroy(gameObject);
    }
}
