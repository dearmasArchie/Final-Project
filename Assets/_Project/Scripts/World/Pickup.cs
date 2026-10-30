using UnityEngine;
using UnityEngine.Events;

namespace SignalLost
{
    /// <summary>Collected with E: Battery Clip, Medkit, Override Fuse, or the Arc Pistol itself.</summary>
    public class Pickup : MonoBehaviour, IInteractable
    {
        public enum Kind { BatteryClip, Medkit, Fuse, ArcPistol }

        [SerializeField] Kind kind = Kind.BatteryClip;
        [Tooltip("Ammo for Battery Clips, HP for Medkits.")]
        [SerializeField] int amount = 12;
        [SerializeField] int fuseNumber = 1;
        [Tooltip("Set for placed pickups so they stay collected. Leave empty for crate drops.")]
        [SerializeField] string persistentId;
        [Tooltip("Optional: replaces the HUD objective when collected.")]
        [SerializeField] string objectiveOnPickup;
        [SerializeField] bool spin = true;

        public UnityEvent onPickedUp = new UnityEvent();

        GameObject lastInteractor;

        public string Prompt
        {
            get
            {
                if (!CanInteract) return kind == Kind.Medkit ? "Health Full" : "Ammo Full";
                switch (kind)
                {
                    case Kind.Medkit: return "Pick up Medkit";
                    case Kind.Fuse: return $"Take Override Fuse {fuseNumber}";
                    case Kind.ArcPistol: return "Take Arc Pistol";
                    default: return "Pick up Battery Clip";
                }
            }
        }

        public bool CanInteract
        {
            get
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) return true;
                if (kind == Kind.Medkit) return player.GetComponent<PlayerHealth>().Current < player.GetComponent<PlayerHealth>().Max;
                if (kind == Kind.BatteryClip) return !player.GetComponent<ArcPistol>().ReserveFull;
                return true;
            }
        }

        void Start()
        {
            if (kind == Kind.Fuse) Sfx.Loop(gameObject, "hum", volumeScale: 0.36f, pitch: 1.3f);
            if (!string.IsNullOrEmpty(persistentId) && GameManager.Instance.IsCleared(persistentId))
                Destroy(gameObject);
        }

        void Update()
        {
            if (spin) transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        public void Interact(GameObject interactor)
        {
            var gm = GameManager.Instance;
            Sfx.Play2D(kind == Kind.Medkit ? "pickup_medkit" : kind == Kind.Fuse ? "pickup_fuse" : kind == Kind.ArcPistol ? "pickup_pistol" : "pickup_battery");
            switch (kind)
            {
                case Kind.BatteryClip: interactor.GetComponent<ArcPistol>().AddAmmo(amount); break;
                case Kind.Medkit: interactor.GetComponent<PlayerHealth>().Heal(amount); break;
                case Kind.Fuse: gm.CollectFuse(fuseNumber); break;
                case Kind.ArcPistol: gm.HasArcPistol = true; break;
            }

            if (!string.IsNullOrEmpty(persistentId)) gm.MarkCleared(persistentId);
            if (!string.IsNullOrEmpty(objectiveOnPickup) && HUDController.Instance != null)
                HUDController.Instance.SetObjective(objectiveOnPickup);
            else if (kind == Kind.ArcPistol)
                ObjectiveManager.RefreshNow();

            onPickedUp.Invoke();
            Destroy(gameObject);
        }
    }
}
