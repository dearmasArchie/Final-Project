using UnityEngine;

namespace SignalLost
{
    /// <summary>The PUMP handle in Wing B: tests the junction lever routing.</summary>
    public class PumpHandle : MonoBehaviour, IInteractable
    {
        [SerializeField] PipeRouting routing;

        public string Prompt => routing.Drained ? null : "Pull PUMP (test routing)";
        public bool CanInteract => !routing.Drained;

        public void Interact(GameObject interactor) => routing.TestRouting();
    }
}
