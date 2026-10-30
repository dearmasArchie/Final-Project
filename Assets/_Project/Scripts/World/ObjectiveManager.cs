using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalLost
{
    /// <summary>
    /// Works out the current objective from game progress and shows it on the HUD.
    /// Refreshes on scene start and whenever a fuse is collected. Scripted moments
    /// (briefing, valve, lockdown) can still set their own text in between.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        static ObjectiveManager instance;

        void Awake() => instance = this;

        void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.FusesChanged += OnFusesChanged;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.FusesChanged -= OnFusesChanged;
            if (instance == this) instance = null;
        }

        void Start() => Refresh();

        void OnFusesChanged(int count) => Refresh();

        /// <summary>Recalculate the objective now (e.g. after the lockdown ends).</summary>
        public static void RefreshNow()
        {
            if (instance != null) instance.Refresh();
        }

        void Refresh()
        {
            if (HUDController.Instance != null) HUDController.Instance.SetObjective(Current());
        }

        static string Current()
        {
            var gm = GameManager.Instance;
            bool onIsland = SceneManager.GetActiveScene().name == "IslandExterior";
            bool fuse1 = gm.HasFuse(1), fuse2 = gm.HasFuse(2);

            if (!gm.IsCleared("Briefing")) return "Talk to Dr. Reyes in the safe room";
            if (!gm.HasArcPistol) return "Take the Arc Pistol from the armory";
            if (!fuse1 || !fuse2)
            {
                if (onIsland) return "Ride the freight elevator down";
                if (!fuse1 && !fuse2) return "Recover Override Fuses 1 (Wing A) and 2 (Wing B)";
                return fuse1 ? "Recover Override Fuse 2 in Wing B" : "Recover Override Fuse 1 in Wing A";
            }
            if (!onIsland) return "Return to the freight elevator";
            if (!gm.HasFuse(3)) return "Climb to the radio tower and take Override Fuse 3";
            return "Insert the fuses at the radio tower console";
        }
    }
}
