using UnityEngine;

namespace SignalLost
{
    /// <summary>One wall breaker in the BreakerPuzzle. The handle rotates down when pulled.</summary>
    public class Lever : MonoBehaviour, IInteractable
    {
        [SerializeField] string label = "MAIN";
        [SerializeField] Transform handle;
        [SerializeField] BreakerPuzzle puzzle;
        [SerializeField] float pulledAngle = 70f;

        bool pulled;

        public string Label => label;
        public string Prompt => pulled ? null : $"Pull {label} Breaker";
        public bool CanInteract => !pulled && !puzzle.Solved;

        void Awake() => SetPulled(false);

        public void Interact(GameObject interactor) => puzzle.OnLeverPulled(this);

        public void SetPulled(bool value)
        {
            pulled = value;
            if (handle != null) handle.localRotation = Quaternion.Euler(value ? pulledAngle : -pulledAngle, 0f, 0f);
        }
    }
}
