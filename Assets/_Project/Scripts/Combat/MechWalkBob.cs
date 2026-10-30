using UnityEngine;
using UnityEngine.AI;

namespace SignalLost
{
    /// <summary>
    /// Code "animation" for the unrigged Titan mech model: a heavy up-down bob and side sway while the NavMeshAgent
    /// walks, and a slow idle breathing motion when it stands. Moves only this (visual) transform, so colliders and
    /// weak points on the root stay where the gameplay expects them.
    /// </summary>
    public class MechWalkBob : MonoBehaviour
    {
        [SerializeField, Tooltip("Steps per second at full walking speed")] float stepRate = 1.1f;
        [SerializeField, Tooltip("Bob height per step (m)")] float bobHeight = 0.12f;
        [SerializeField, Tooltip("Side-to-side roll per step (degrees)")] float swayDegrees = 2.5f;
        [SerializeField, Tooltip("Agent speed that counts as full walking speed")] float fullSpeed = 1.2f;

        NavMeshAgent agent;
        Vector3 restPosition;
        Quaternion restRotation;
        float phase;
        float walkBlend;

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

            // Walking: a sharp "footfall" bob twice per stride cycle and a roll toward the planted foot.
            // Standing: a slow, shallow breathing motion.
            float bob = Mathf.Lerp(Mathf.Sin(phase) * 0.02f, -Mathf.Abs(Mathf.Sin(phase)) * bobHeight, walkBlend);
            float roll = Mathf.Sin(phase) * swayDegrees * walkBlend;
            transform.localPosition = restPosition + Vector3.up * bob;
            transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, roll);
        }
    }
}
