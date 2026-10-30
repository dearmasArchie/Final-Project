using UnityEngine;

namespace SignalLost
{
    /// <summary>Radio tower console: insert the override fuses one at a time. All three start the upload.</summary>
    public class FuseConsole : MonoBehaviour, IInteractable
    {
        [SerializeField] WaveSpawner upload;
        [SerializeField] Renderer[] fuseSlots;
        [SerializeField] Material slotFilled;

        static string SlotId(int fuse) => $"FuseInserted_{fuse}";

        int Inserted
        {
            get
            {
                int count = 0;
                for (int i = 1; i <= GameManager.FuseCount; i++) if (GameManager.Instance.IsCleared(SlotId(i))) count++;
                return count;
            }
        }

        int NextInsertable
        {
            get
            {
                var gm = GameManager.Instance;
                for (int i = 1; i <= GameManager.FuseCount; i++)
                    if (gm.HasFuse(i) && !gm.IsCleared(SlotId(i))) return i;
                return 0;
            }
        }

        public string Prompt
        {
            get
            {
                if (Inserted == GameManager.FuseCount) return null;
                return NextInsertable > 0
                    ? $"Insert Override Fuse {NextInsertable} ({Inserted}/{GameManager.FuseCount})"
                    : $"Needs Override Fuses ({Inserted}/{GameManager.FuseCount} inserted)";
            }
        }

        public bool CanInteract => NextInsertable > 0;

        void Start() => RefreshSlots();

        public void Interact(GameObject interactor)
        {
            GameManager.Instance.MarkCleared(SlotId(NextInsertable));
            RefreshSlots();
            if (Inserted == GameManager.FuseCount)
            {
                Radio.Say("All three fuses are in! The dish is aligning – hold the compound until the upload finishes!");
                upload.Begin();
            }
        }

        void RefreshSlots()
        {
            for (int i = 0; i < fuseSlots.Length; i++)
                if (GameManager.Instance.IsCleared(SlotId(i + 1))) fuseSlots[i].sharedMaterial = slotFilled;
        }
    }
}
