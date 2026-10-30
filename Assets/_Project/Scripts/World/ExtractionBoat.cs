using UnityEngine;

namespace SignalLost
{
    /// <summary>The rescue boat at the pier. Boarding it after the Titan is destroyed wins the game.</summary>
    public class ExtractionBoat : MonoBehaviour, IInteractable
    {
        bool Ready => GameManager.Instance.IsCleared(BossController.DefeatedId);

        public string Prompt => Ready ? "Board Extraction Boat" : "The Titan is blocking the dock";
        public bool CanInteract => Ready;

        public void Interact(GameObject interactor)
        {
            GameManager.Instance.EndRun();
            MenuController.Instance.ShowVictory();
        }
    }
}
