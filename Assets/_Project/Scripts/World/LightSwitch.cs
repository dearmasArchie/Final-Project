using UnityEngine;

namespace SignalLost
{
    /// <summary>Wall switch that turns a room's lights on and off (the maintenance bunker's puzzle uses it).</summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] Light[] lights;
        [SerializeField] Transform lever;
        [SerializeField] ArmoryCodePuzzle puzzle;

        bool on = true;

        public string Prompt => on ? "Turn Lights Off" : "Turn Lights On";
        public bool CanInteract => true;

        public void Interact(GameObject interactor)
        {
            on = !on;
            foreach (var l in lights) l.enabled = on;
            if (lever != null) lever.localRotation = Quaternion.Euler(on ? -35f : 35f, 0f, 0f);
            if (puzzle != null) puzzle.SetLights(on);
        }
    }
}
