using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Anything the player can use with E: doors, keypads, levers, valves, pickups, NPCs, the elevator.
    /// PlayerInteraction (Partner A) raycasts 3 m from the camera, shows Prompt, and calls Interact on E.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Text shown under the crosshair, without the key, e.g. "Turn Drainage Valve".
        /// When CanInteract is false, return the reason instead, e.g. "Requires: Arc Pistol".
        /// </summary>
        string Prompt { get; }

        /// <summary>False when the object is locked or already used. The prompt is still shown.</summary>
        bool CanInteract { get; }

        /// <summary>Called once when the player presses E while looking at this object and CanInteract is true.</summary>
        void Interact(GameObject interactor);
    }
}
