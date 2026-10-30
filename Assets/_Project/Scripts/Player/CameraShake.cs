using UnityEngine;

namespace SignalLost
{
    /// <summary>Short camera shakes (Overload Beam, taking damage, explosions). Offsets position only, so mouse look is untouched.</summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        Vector3 basePosition;
        float strength;
        float timeLeft;
        float duration;

        void Awake()
        {
            Instance = this;
            basePosition = transform.localPosition;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Shake(float amount, float seconds)
        {
            if (amount < strength && timeLeft > 0f) return;
            strength = amount;
            duration = timeLeft = seconds;
        }

        void LateUpdate()
        {
            if (timeLeft <= 0f)
            {
                transform.localPosition = basePosition;
                return;
            }
            timeLeft -= Time.deltaTime;
            float fade = Mathf.Clamp01(timeLeft / duration);
            transform.localPosition = basePosition + Random.insideUnitSphere * strength * fade;
        }
    }
}
