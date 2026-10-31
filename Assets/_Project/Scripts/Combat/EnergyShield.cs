using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Front energy shield (Aegis Warden, Titan riot shield). Blocks Pulse shots completely.
    /// An Overload Beam or explosion breaks it for a few seconds, then it comes back.
    /// Put it on a collider in front of the body, so shots from the side or back get past it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnergyShield : MonoBehaviour, IDamageable
    {
        [SerializeField] float downTime = 6f;
        [SerializeField] Renderer shieldRenderer;

        Collider shieldCollider;
        float upAgainTime;

        public bool IsUp { get; private set; } = true;

        void Awake() => shieldCollider = GetComponent<Collider>();

        void Update()
        {
            if (!IsUp && Time.time >= upAgainTime) SetUp(true);
        }

        public void TakeDamage(float amount, DamageType type, Vector3 hitPoint)
        {
            if (!IsUp || type == DamageType.Pulse || type == DamageType.Hazard) return;
            upAgainTime = Time.time + downTime;
            SetUp(false);
        }

        /// <summary>The owner died: the shield disappears at once and never comes back.</summary>
        public void Dismantle()
        {
            IsUp = false;
            enabled = false;
            if (shieldCollider == null) shieldCollider = GetComponent<Collider>();
            shieldCollider.enabled = false;
            if (shieldRenderer != null) shieldRenderer.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        void SetUp(bool up)
        {
            if (IsUp && !up) Sfx.Play("shield_break", transform.position);
            IsUp = up;
            shieldCollider.enabled = up;
            if (shieldRenderer != null) shieldRenderer.enabled = up;
        }
    }
}
