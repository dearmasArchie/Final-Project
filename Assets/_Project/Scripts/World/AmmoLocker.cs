using UnityEngine;

namespace SignalLost
{
    /// <summary>Refills the Arc Pistol's reserve. Recharges for a while after each use, so ammo can never run out for good.</summary>
    public class AmmoLocker : MonoBehaviour, IInteractable
    {
        [SerializeField] int refillAmount = ArcPistol.MaxReserve;
        [SerializeField] float rechargeTime = 20f;

        float readyTime;

        bool Ready => Time.time >= readyTime;

        public string Prompt => Ready ? "Refill Batteries" : $"Recharging ({Mathf.CeilToInt(readyTime - Time.time)} s)";
        public bool CanInteract => Ready && GameManager.Instance.HasArcPistol;

        public void Interact(GameObject interactor)
        {
            if (interactor.GetComponent<ArcPistol>().AddAmmo(refillAmount))
                readyTime = Time.time + rechargeTime;
        }
    }
}
