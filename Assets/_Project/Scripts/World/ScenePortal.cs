using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// The freight elevator panel: loads another scene and arrives at a spawn point there.
    /// Can require the Arc Pistol or Fuses 1 and 2, plus a cleared progress ID
    /// (main power on the island, the rewired junction box in the bunker).
    /// The optional status light shows red while locked and green when usable.
    /// </summary>
    public class ScenePortal : MonoBehaviour, IInteractable
    {
        public enum Requirement { None, ArcPistol, FusesOneAndTwo }

        [SerializeField] string prompt = "Ride Elevator Down";
        [SerializeField] string targetScene = "UndergroundFacility";
        [SerializeField] string targetSpawnId = "ElevatorBottom";
        [SerializeField] Requirement requirement = Requirement.None;
        [Tooltip("Also needs this progress ID cleared, e.g. ElevatorPowered or ElevatorCircuitRestored. Empty = none.")]
        [SerializeField] string requiredClearedId;
        [SerializeField] string missingClearedMessage = "No power";
        [SerializeField] Renderer statusLight;

        MaterialPropertyBlock block;
        bool? shownUsable;

        bool RequirementMet
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

        bool ProgressMet => string.IsNullOrEmpty(requiredClearedId) || GameManager.Instance.IsCleared(requiredClearedId);

        public bool CanInteract => RequirementMet && ProgressMet;

        public string Prompt
        {
            get
            {
                if (!RequirementMet)
                    return requirement == Requirement.ArcPistol
                        ? "Requires: Arc Pistol"
                        : "Requires: Override Fuses 1 and 2";
                if (!ProgressMet) return missingClearedMessage;
                return prompt;
            }
        }

        void Update()
        {
            if (statusLight == null) return;
            bool usable = CanInteract;
            if (shownUsable == usable) return;
            shownUsable = usable;
            block ??= new MaterialPropertyBlock();
            var color = usable ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.15f, 0.1f);
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", color * 3f);
            statusLight.SetPropertyBlock(block);
        }

        public void Interact(GameObject interactor)
        {
            GameManager.Instance.NextSpawnPointId = targetSpawnId;
            SceneLoader.Instance.Load(targetScene);
        }
    }
}
