using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Wing B drain controls. Three junction levers route the floodwater; the pipe schematic on the wall lights up
    /// the path the water would take with the current lever settings. Pulling the pump handle sends the water:
    /// reaching the DRAIN empties the electrified floodway and unlocks the quarantine door; a RETURN pipe surges back.
    /// </summary>
    public class PipeRouting : MonoBehaviour
    {
        [SerializeField] PipeLever[] levers;
        [Tooltip("Correct position per lever: true = DOWN branch. Must match the painted schematic.")]
        [SerializeField] bool[] solutionDown = { true, false, true };
        [SerializeField] ElectrifiedWater water;
        [SerializeField] Door doorToUnlock;
        [SerializeField] Transform surgePoint;
        [SerializeField] string objectiveAfter = "Retrieve Override Fuse 2 from the quarantine room";

        [Header("Schematic pipes (lit along the current route)")]
        [SerializeField] Renderer[] inletPipes;
        [SerializeField] Renderer[] aUpPipes;
        [SerializeField] Renderer[] aDownPipes;
        [SerializeField] Renderer[] bUpPipes;
        [SerializeField] Renderer[] bDownPipes;
        [SerializeField] Renderer[] cUpPipes;
        [SerializeField] Renderer[] cDownPipes;
        [SerializeField] Color flowColor = new Color(0.3f, 0.9f, 1f);

        static readonly string[] JunctionNames = { "A", "B", "C" };
        MaterialPropertyBlock lit;

        public bool Drained => water.Drained;
        public bool[] Solution => solutionDown;

        void Start()
        {
            lit = new MaterialPropertyBlock();
            lit.SetColor("_BaseColor", flowColor);
            lit.SetColor("_EmissionColor", flowColor * 3f);

            if (GameManager.Instance.IsCleared("FloodwayDrained"))
            {
                for (int i = 0; i < levers.Length; i++) levers[i].SetDown(solutionDown[i]);
                doorToUnlock.Unlock();
            }
            RefreshPreview();
        }

        /// <summary>Called by a lever after it flips.</summary>
        public void OnLeverChanged() => RefreshPreview();

        /// <summary>Called by the pump handle.</summary>
        public void TestRouting()
        {
            if (Drained) return;

            int returnedAt = Route(out _);
            if (returnedAt < 0)
            {
                water.Drain();
                doorToUnlock.Unlock();
                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowToast("FLOW ROUTED TO DRAIN");
                    HUDController.Instance.SetObjective(objectiveAfter);
                }
            }
            else
            {
                if (surgePoint != null) ExplosionFlash.Spawn(surgePoint.position, 1.2f);
                if (HUDController.Instance != null)
                    HUDController.Instance.ShowToast($"WATER RETURNED AT JUNCTION {JunctionNames[returnedAt]} – IT NEEDS TO REACH THE DRAIN");
            }
        }

        /// <summary>
        /// Follows the water through the junctions with the current lever settings.
        /// Returns the junction index where it turns back into the room, or -1 if it reaches the drain.
        /// </summary>
        int Route(out Renderer[][] litBranches)
        {
            bool aDown = levers[0].IsDown, bDown = levers[1].IsDown, cDown = levers[2].IsDown;
            // Schematic: A up = return, A down → B; B up → C, B down = return; C up = return, C down = drain.
            if (!aDown) { litBranches = new[] { inletPipes, aUpPipes }; return 0; }
            if (bDown) { litBranches = new[] { inletPipes, aDownPipes, bDownPipes }; return 1; }
            if (!cDown) { litBranches = new[] { inletPipes, aDownPipes, bUpPipes, cUpPipes }; return 2; }
            litBranches = new[] { inletPipes, aDownPipes, bUpPipes, cDownPipes };
            return -1;
        }

        void RefreshPreview()
        {
            if (lit == null) return;
            foreach (var branch in new[] { inletPipes, aUpPipes, aDownPipes, bUpPipes, bDownPipes, cUpPipes, cDownPipes })
                foreach (var pipe in branch) pipe.SetPropertyBlock(null);
            Route(out var litBranches);
            foreach (var branch in litBranches)
                foreach (var pipe in branch) pipe.SetPropertyBlock(lit);
        }

        /// <summary>Automated tests only: set the levers and run the pump.</summary>
        public void SetLeversAndTest(bool[] down)
        {
            for (int i = 0; i < levers.Length; i++) levers[i].SetDown(down[i]);
            RefreshPreview();
            TestRouting();
        }
    }
}
