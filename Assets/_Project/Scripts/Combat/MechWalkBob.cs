using UnityEngine;
using UnityEngine.AI;

namespace SignalLost
{
    /// <summary>
    /// Walk animation for the unrigged Titan mech. The model is rigid pieces, so the legs are swung from hip, knee and ankle
    /// pivots (see TitanRigBuilder): a stride with the knee lifting on the forward swing and the foot kept level, plus a heavy
    /// body bob and sway. When it stands the legs settle and the body breathes slowly. Moves only the visual transforms, so
    /// colliders and weak points on the root stay where the gameplay expects them.
    /// </summary>
    public class MechWalkBob : MonoBehaviour
    {
        [SerializeField, Tooltip("Full strides (left + right) per second at full walking speed")] float stepRate = 0.65f;
        [SerializeField, Tooltip("Body bob per step (m)")] float bobHeight = 0.07f;
        [SerializeField, Tooltip("Side-to-side roll per step (degrees)")] float swayDegrees = 1.5f;
        [SerializeField, Tooltip("Agent speed that counts as full walking speed")] float fullSpeed = 1.2f;
        [SerializeField, Tooltip("How far each leg swings forward and back at the hip (degrees)")] float hipSwing = 13f;
        [SerializeField, Tooltip("Extra knee bend while a leg swings forward to lift the foot (degrees)")] float kneeLift = 24f;

        [SerializeField] Transform leftHip, leftKnee, leftAnkle, rightHip, rightKnee, rightAnkle;

        NavMeshAgent agent;
        Vector3 restPosition;
        Quaternion restRotation;
        float phase;
        float walkBlend;
        int lastStep;

        void Awake()
        {
            agent = GetComponentInParent<NavMeshAgent>();
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
        }

        void Update()
        {
            float speed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
            walkBlend = Mathf.MoveTowards(walkBlend, Mathf.Clamp01(speed / fullSpeed), Time.deltaTime * 2f);
            phase += Time.deltaTime * Mathf.Lerp(0.25f, stepRate, walkBlend) * Mathf.PI * 2f;

            // A footfall each time the bob reaches its lowest point (phase = pi/2 + k*pi)
            int step = Mathf.FloorToInt((phase - Mathf.PI / 2f) / Mathf.PI);
            if (step != lastStep)
            {
                lastStep = step;
                if (walkBlend > 0.4f) Sfx.Play("titan_step", transform.position);
            }

            // Walking: a sharp "footfall" bob twice per stride cycle and a roll toward the planted foot.
            // Standing: a slow, shallow breathing motion.
            float bob = Mathf.Lerp(Mathf.Sin(phase) * 0.02f, -Mathf.Abs(Mathf.Sin(phase)) * bobHeight, walkBlend);
            float roll = Mathf.Sin(phase) * swayDegrees * walkBlend;
            transform.localPosition = restPosition + Vector3.up * bob;
            transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, roll);

            AnimateLeg(leftHip, leftKnee, leftAnkle, phase);
            AnimateLeg(rightHip, rightKnee, rightAnkle, phase + Mathf.PI);
        }

        /// <summary>
        /// One leg at a point in the stride. The leg is forward and planted at the top of the sine and swinging forward while its
        /// cosine is positive. Rotating about +X by a negative angle moves the foot forward (+Z).
        /// </summary>
        void AnimateLeg(Transform hip, Transform knee, Transform ankle, float legPhase)
        {
            if (hip == null) return;
            float swingForward = Mathf.Cos(legPhase);
            float hipAngle = -Mathf.Sin(legPhase) * hipSwing * walkBlend;
            float kneeAngle = Mathf.Max(0f, swingForward) * kneeLift * walkBlend;   // bends back to lift the foot
            hip.localRotation = Quaternion.Euler(hipAngle, 0f, 0f);
            if (knee != null) knee.localRotation = Quaternion.Euler(kneeAngle, 0f, 0f);
            if (ankle != null) ankle.localRotation = Quaternion.Euler(-(hipAngle + kneeAngle) * 0.9f, 0f, 0f); // keep the foot flat
        }
    }
}
