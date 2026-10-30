using UnityEngine;

namespace SignalLost
{
    /// <summary>Wooden supply crate: breaks when shot and may drop a Battery Clip (60%) or Medkit (25%).</summary>
    public class Destructible : MonoBehaviour, IDamageable
    {
        [SerializeField] float health = 20f;
        [SerializeField] GameObject batteryPrefab;
        [SerializeField] GameObject medkitPrefab;
        [SerializeField, Range(0f, 1f)] float batteryChance = 0.6f;
        [SerializeField, Range(0f, 1f)] float medkitChance = 0.25f;

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            health -= amount;
            if (health > 0f) return;

            float roll = Random.value;
            GameObject drop = roll < batteryChance ? batteryPrefab
                : roll < batteryChance + medkitChance ? medkitPrefab
                : null;
            if (drop != null) Instantiate(drop, transform.position + Vector3.up * 0.3f, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
