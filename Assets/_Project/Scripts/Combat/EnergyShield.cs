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

        void SetUp(bool up)
        {
            IsUp = up;
            shieldCollider.enabled = up;
            if (shieldRenderer != null) shieldRenderer.enabled = up;
        }
    }
}
