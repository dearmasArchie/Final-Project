using UnityEngine;

namespace SignalLost
{
    /// <summary>An enemy's weapon. EnemyAI calls Tick every frame while in the Attack state.</summary>
    public abstract class EnemyAttack : MonoBehaviour
    {
        /// <summary>Called every frame in the Attack state. target is the player's center of mass.</summary>
        public abstract void Tick(EnemyAI ai, Vector3 target);

        /// <summary>Called when the enemy leaves the Attack state, is stunned, or dies.</summary>
        public virtual void Stop() { }
    }
}
