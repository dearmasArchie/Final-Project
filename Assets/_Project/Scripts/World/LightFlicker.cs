using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Hazard lighting: a smooth noise flicker (electrified water, sparking wiring) or a steady pulse (alarm beacons).
    /// Scales the light's starting intensity, so the Inspector value is the brightest it gets.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public enum Mode { Flicker, Pulse }

        [SerializeField] Mode mode = Mode.Flicker;
        [SerializeField, Tooltip("Flickers or pulses per second")] float speed = 6f;
        [SerializeField, Range(0f, 1f), Tooltip("Lowest brightness as a fraction of the full intensity")] float minimum = 0.35f;

        Light target;
        float baseIntensity;
        float seed;

        void Awake()
        {
            target = GetComponent<Light>();
            baseIntensity = target.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float t = mode == Mode.Flicker
                ? Mathf.PerlinNoise(seed, Time.time * speed)
                : 0.5f + 0.5f * Mathf.Sin((Time.time + seed) * speed * Mathf.PI * 2f);
            target.intensity = baseIntensity * Mathf.Lerp(minimum, 1f, t);
        }
    }
}
