using TMPro;
using UnityEngine;

namespace SignalLost
{
    /// <summary>One junction lever (A, B or C) in the Wing B drain controls. Flips between the UP and DOWN branch.</summary>
    public class PipeLever : MonoBehaviour, IInteractable
    {
        [SerializeField] string label = "A";
        [SerializeField] Transform handle;
        [SerializeField] PipeRouting routing;
        [SerializeField] TMP_Text stateLabel;

        public bool IsDown { get; private set; }

        public string Prompt => routing.Drained ? null : $"Flip Junction {label} to {(IsDown ? "UP" : "DOWN")}";
        public bool CanInteract => !routing.Drained;

        void Awake() => SetDown(false);

        public void Interact(GameObject interactor)
        {
            SetDown(!IsDown);
            routing.OnLeverChanged();
        }

        public void SetDown(bool down)
        {
            IsDown = down;
            if (handle != null) handle.localRotation = Quaternion.Euler(down ? -50f : 50f, 0f, 0f); // arm points at the player: +50 lifts it, -50 lowers it
            if (stateLabel != null) stateLabel.text = $"{label}: {(down ? "DOWN" : "UP")}";
        }
    }
}
