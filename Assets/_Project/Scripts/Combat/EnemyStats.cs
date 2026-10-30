using UnityEngine;

namespace SignalLost
{
    /// <summary>Tuning numbers for one enemy type. HP lives on the Health component; attack numbers on the attack component.</summary>
    [CreateAssetMenu(menuName = "Signal Lost/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Movement")]
        public float patrolSpeed = 2f;
        public float chaseSpeed = 3.5f;
        [Tooltip("Side-to-side distance while chasing (Recon Stinger zig-zag). 0 = straight.")]
        public float zigZagAmplitude = 0f;
        public float zigZagFrequency = 2f;
        [Tooltip("Keep closing in while attacking (Stinger) instead of holding position (Enforcer).")]
        public bool advanceWhileAttacking = false;
        public float preferredAttackDistance = 7f;
        [Tooltip("Step sideways when hit while attacking (Enforcer).")]
        public bool sideStepWhenHit = true;

        [Header("Senses")]
        public float visionRange = 15f;
        public float visionAngle = 90f;
        public float hearingRange = 15f;
        public float squadAlertRadius = 15f;
        public float loseTargetTime = 5f;

        [Header("Combat")]
        public float attackRange = 10f;
        public float stunDuration = 0.5f;
    }
}
