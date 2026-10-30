using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Turns its child objects on only at a certain stage of the run, checked when the scene starts:
    /// e.g. the hill-trail reinforcements once Fuses 1 and 2 are held.
    /// </summary>
    public class ProgressGate : MonoBehaviour
    {
        public enum Stage { HasFusesOneAndTwo, UploadComplete }

        [SerializeField] Stage activeFrom = Stage.HasFusesOneAndTwo;

        void Start()
        {
            var gm = GameManager.Instance;
            bool active = activeFrom == Stage.UploadComplete
                ? gm.IsCleared(WaveSpawner.CompletedId)
                : gm.HasFuse(1) && gm.HasFuse(2);
            SetChildren(active);
        }

        /// <summary>Turn the children on now (e.g. the boss when the upload completes).</summary>
        public void Activate() => SetChildren(true);

        void SetChildren(bool active)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(active);
        }
    }
}
