using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Wall-mounted laser emitter (put this on the emitter box). Touching a beam deals damage and sounds the alarm.
    /// One shot on the emitter switches all its beams off.
    /// </summary>
    public class LaserTripwire : MonoBehaviour, IDamageable
    {
        [SerializeField] GameObject[] beams;
        [SerializeField] Renderer emitterLight;
        [SerializeField] float touchDamage = 25f;
        [SerializeField] float touchCooldown = 1f;
        [SerializeField] AlarmSpawner alarm;
        [SerializeField] GameObject alarmEnemyPrefab;

        float nextTouchTime;

        public bool Active { get; private set; } = true;

        void Awake()
        {
            foreach (var beam in beams)
            {
                beam.AddComponent<LaserBeam>().owner = this;
                // A kinematic Rigidbody makes trigger events reliable with the player's CharacterController.
                beam.AddComponent<Rigidbody>().isKinematic = true;
            }
        }

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (!Active) return;
            Active = false;
            foreach (var beam in beams) beam.SetActive(false);
            if (emitterLight != null) emitterLight.material.SetColor("_EmissionColor", Color.black);
        }

        /// <summary>Called by LaserBeam when something touches a beam.</summary>
        public void OnBeamTouched(Collider other)
        {
            if (!Active || Time.time < nextTouchTime) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            nextTouchTime = Time.time + touchCooldown;
            health.TakeDamage(touchDamage, DamageType.Hazard, other.transform.position);
            if (alarm != null && alarmEnemyPrefab != null) alarm.Spawn(alarmEnemyPrefab);
        }
    }
}
