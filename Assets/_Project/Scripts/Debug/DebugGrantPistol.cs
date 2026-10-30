using UnityEngine;

namespace SignalLost.DebugTools
{
    /// <summary>Test scenes only: gives the player the Arc Pistol immediately, skipping the armory pickup.</summary>
    public class DebugGrantPistol : MonoBehaviour
    {
        void Start() => GameManager.Instance.HasArcPistol = true;
    }
}
