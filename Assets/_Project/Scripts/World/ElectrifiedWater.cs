using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Trigger volume over the flooded generator floor: 10 HP per second while the player stands in it,
    /// with a HUD warning. Drain() lowers the water and makes it safe.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ElectrifiedWater : MonoBehaviour
    {
        [SerializeField] Transform waterVisual;
        [SerializeField] float damagePerSecond = 10f;
        [SerializeField] float tickInterval = 0.25f;
        [SerializeField] float drainDepth = 0.5f;
        [SerializeField] float drainSpeed = 0.25f;
        [SerializeField] string persistentId = "FloodwayDrained";

        float tickTimer;
        bool playerInside;
        Vector3 drainedPosition;

        public bool Drained { get; private set; }

        void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            // A kinematic Rigidbody makes trigger events reliable with the player's CharacterController.
            if (!TryGetComponent<Rigidbody>(out var body)) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            drainedPosition = waterVisual.localPosition + Vector3.down * drainDepth;
            if (GameManager.Instance.IsCleared(persistentId)) Drain(instant: true);
        }

        void Update()
        {
            if (Drained && waterVisual.gameObject.activeSelf)
            {
                waterVisual.localPosition = Vector3.MoveTowards(waterVisual.localPosition, drainedPosition, drainSpeed * Time.deltaTime);
                if (waterVisual.localPosition == drainedPosition) waterVisual.gameObject.SetActive(false);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (Drained) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            SetWarning(true);
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = tickInterval;
            health.TakeDamage(damagePerSecond * tickInterval, DamageType.Hazard, other.transform.position);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null) SetWarning(false);
        }

        public void Drain(bool instant = false)
        {
            if (Drained) return;
            Drained = true;
            SetWarning(false);
            GameManager.Instance.MarkCleared(persistentId);
            if (instant) waterVisual.gameObject.SetActive(false);
        }

        void SetWarning(bool on)
        {
            if (playerInside == on) return;
            playerInside = on;
            if (HUDController.Instance != null) HUDController.Instance.SetHazardWarning(on ? "!! ELECTRIFIED WATER !!" : null);
        }
    }
}
