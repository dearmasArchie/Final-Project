using UnityEngine;

namespace SignalLost
{
    /// <summary>The drainage valve in Wing B: drains the electrified water and unlocks the quarantine room door.</summary>
    public class Valve : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform wheel;
        [SerializeField] ElectrifiedWater water;
        [SerializeField] Door doorToUnlock;
        [SerializeField] string objectiveOnTurn = "Retrieve Override Fuse 2 from the quarantine room";

        bool turning;

        public string Prompt => water.Drained ? null : "Turn Drainage Valve";
        public bool CanInteract => !water.Drained;

        void Start()
        {
            if (water.Drained || GameManager.Instance.IsCleared("FloodwayDrained")) doorToUnlock.Unlock();
        }

        void Update()
        {
            if (turning) wheel.Rotate(0f, 180f * Time.deltaTime, 0f, Space.Self);
        }

        public void Interact(GameObject interactor)
        {
            water.Drain();
            doorToUnlock.Unlock();
            turning = true;
            Invoke(nameof(StopTurning), 2f);
            if (HUDController.Instance != null) HUDController.Instance.SetObjective(objectiveOnTurn);
        }

        void StopTurning() => turning = false;
    }
}
