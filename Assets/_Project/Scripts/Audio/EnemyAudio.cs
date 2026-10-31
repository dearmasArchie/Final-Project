using UnityEngine;
using UnityEngine.AI;

namespace SignalLost
{
    /// <summary>Quiet 3D servo hum on a robot that rises in pitch and volume as it moves. Added by EnemyAI.</summary>
    public class EnemyAudio : MonoBehaviour
    {
        NavMeshAgent agent;
        AudioSource servo;
        float basePitch;
        float gain = 1f;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            // Hovering drones whine higher than walking bots.
            // Enforcers and Stingers hum at 70%; the Aegis Warden keeps full volume.
            gain = name.Contains("Aegis") ? 1f : 0.7f;
            basePitch = agent != null && agent.baseOffset > 0.5f ? 1.5f : 1f;
            servo = Sfx.Loop(gameObject, "servo", autoplay: true, volumeScale: 0.3f, pitch: basePitch);
        }

        void Update()
        {
            if (servo == null || !servo.isPlaying) return; // stopped when the robot dies
            float speed01 = agent != null && agent.enabled ? Mathf.Clamp01(agent.velocity.magnitude / 5f) : 0f;
            servo.pitch = basePitch * (0.8f + 0.5f * speed01);
            servo.volume = Mathf.Lerp(0.15f, 0.5f, speed01) * gain;
        }
    }
}
