using UnityEngine;
using UnityEngine.Events;

namespace SignalLost
{
    /// <summary>
    /// Main power lever in the generator bunker. Turning it on powers the freight elevator for the rest of the run.
    /// </summary>
    public class PowerSwitch : MonoBehaviour, IInteractable
    {
        public const string PoweredId = "ElevatorPowered";

        [SerializeField] Transform lever;
        [SerializeField] Light[] lightsToTurnOn;
        [SerializeField] float onAngle = -60f;
        [SerializeField] float offAngle = 60f;

        public UnityEvent onPoweredOn = new UnityEvent();

        static bool Powered => GameManager.Instance.IsCleared(PoweredId);

        public string Prompt => Powered ? null : "Turn Main Power On";
        public bool CanInteract => !Powered;

        void Start() => SetVisuals(Powered);

        public void Interact(GameObject interactor)
        {
            GameManager.Instance.MarkCleared(PoweredId);
            Sfx.Play("power_on", transform.position);
            SetVisuals(true);
            ExplosionFlash.Spawn(transform.position, 0.6f); // spark
            if (HUDController.Instance != null) HUDController.Instance.ShowToast("POWER RESTORED");
            onPoweredOn.Invoke();
            ObjectiveManager.RefreshNow();
        }

        void SetVisuals(bool on)
        {
            if (lever != null) lever.localRotation = Quaternion.Euler(0f, 0f, on ? onAngle : offAngle);
            foreach (var l in lightsToTurnOn) l.enabled = on;
        }
    }
}
