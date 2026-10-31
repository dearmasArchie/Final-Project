using UnityEngine;

namespace SignalLost
{
    /// <summary>Wall switch that turns a room's lights on and off (the maintenance bunker's puzzle uses it).</summary>
    public class LightSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] Light[] lights;
        [SerializeField] Transform lever;
        [Tooltip("Axis the toggle swings around, in the lever's parent space.")]
        [SerializeField] Vector3 leverAxis = Vector3.right;
        [SerializeField] float onAngle = -35f;
        [SerializeField] float offAngle = 35f;
        [SerializeField] ArmoryCodePuzzle puzzle;

        bool on = true;

        public string Prompt => on ? "Turn Lights Off" : "Turn Lights On";
        public bool CanInteract => true;

        public void Interact(GameObject interactor)
        {
            on = !on;
            foreach (var l in lights) l.enabled = on;
            if (lever != null) lever.localRotation = Quaternion.AngleAxis(on ? onAngle : offAngle, leverAxis);
            if (puzzle != null) puzzle.SetLights(on);
        }
    }
}
