using UnityEngine;

namespace SignalLost
{
    /// <summary>What caused the damage. Shields use this: they ignore Pulse and break on Overload or Explosion.</summary>
    public enum DamageType
    {
        Pulse,      // Arc Pistol primary fire
        Overload,   // Arc Pistol charged beam
        Explosion,  // Red barrels
        Hazard      // Lasers, electrified water, enemy attacks on the player
    }

    /// <summary>Anything that can take damage: the player, enemies, the boss, pylons, crates, barrels, laser emitters.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, DamageType type, Vector3 hitPoint);
    }
}
