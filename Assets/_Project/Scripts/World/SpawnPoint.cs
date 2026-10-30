using System.Collections.Generic;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// A place the player can arrive at after a scene load, e.g. "SafeRoom", "ElevatorTop", "ElevatorBottom".
    /// The blue arrow in the Scene view shows which way the player will face.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        static readonly List<SpawnPoint> active = new List<SpawnPoint>();

        [SerializeField] string id = "SafeRoom";
        [Tooltip("Used when no spawn ID was requested, e.g. starting a new run.")]
        [SerializeField] bool isDefault;

        public string Id => id;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        /// <summary>Moves the object tagged "Player" to the matching spawn point (or the default one).</summary>
        public static void PlacePlayer(string spawnId)
        {
            SpawnPoint target = null;
            foreach (var point in active)
            {
                if (!string.IsNullOrEmpty(spawnId) && point.id == spawnId) { target = point; break; }
                if (point.isDefault && target == null) target = point;
            }
            if (target == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // A CharacterController overrides transform changes while enabled.
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            if (controller != null) controller.enabled = true;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = isDefault ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 1.5f);
        }
    }
}
