using UnityEngine;

namespace SignalLost
{
    /// <summary>The freight elevator panel: loads another scene and arrives at a spawn point there.</summary>
    public class ScenePortal : MonoBehaviour, IInteractable
    {
        public enum Requirement { None, ArcPistol, FusesOneAndTwo }

        [SerializeField] string prompt = "Ride Elevator Down";
        [SerializeField] string targetScene = "UndergroundFacility";
        [SerializeField] string targetSpawnId = "ElevatorBottom";
        [SerializeField] Requirement requirement = Requirement.None;

        public bool CanInteract
        {
            get
            {
                var gm = GameManager.Instance;
                switch (requirement)
                {
                    case Requirement.ArcPistol: return gm.HasArcPistol;
                    case Requirement.FusesOneAndTwo: return gm.HasFuse(1) && gm.HasFuse(2);
                    default: return true;
                }
            }
        }

        public string Prompt
        {
            get
            {
                if (CanInteract) return prompt;
                return requirement == Requirement.ArcPistol
                    ? "Requires: Arc Pistol"
                    : "Requires: Override Fuses 1 and 2";
            }
        }

        public void Interact(GameObject interactor)
        {
            GameManager.Instance.NextSpawnPointId = targetSpawnId;
            SceneLoader.Instance.Load(targetScene);
        }
    }
}
