using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Safety net against falling out of the map: remembers the last spot the player stood on solid ground
    /// (outside the electrified water) and puts them back there if they drop far below the world.
    /// Added to the player automatically by FirstPersonController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FallRecovery : MonoBehaviour
    {
        [SerializeField] float killHeight = -25f;
        [SerializeField] float recordInterval = 0.5f;

        CharacterController controller;
        Vector3 lastSafe;
        float nextRecord;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastSafe = transform.position;
        }

        void Update()
        {
            if (controller.isGrounded && Time.time >= nextRecord)
            {
                nextRecord = Time.time + recordInterval;
                lastSafe = transform.position;
            }

            if (transform.position.y < killHeight) Restore();
        }

        void Restore()
        {
            controller.enabled = false; // a CharacterController overrides direct position changes while enabled
            transform.position = lastSafe + Vector3.up * 0.5f;
            controller.enabled = true;
        }
    }
}
