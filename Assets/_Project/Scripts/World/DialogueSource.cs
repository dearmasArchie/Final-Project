using UnityEngine;
using UnityEngine.Events;

namespace SignalLost
{
    /// <summary>
    /// Shows lines in the HUD dialogue box, one per E press or click: Dr. Reyes's briefing, terminals, notes.
    /// The player can't move or shoot while it is open.
    /// </summary>
    public class DialogueSource : MonoBehaviour, IInteractable
    {
        [SerializeField] string speaker = "Dr. Reyes";
        [SerializeField] string prompt = "Talk to Dr. Reyes";
        [SerializeField, TextArea(2, 4)] string[] lines = { "..." };
        [Tooltip("Shown instead of the full lines after the first time (optional).")]
        [SerializeField, TextArea(2, 4)] string[] repeatLines;
        [Tooltip("Marked cleared in GameManager when the dialogue first finishes (e.g. \"Briefing\").")]
        [SerializeField] string persistentId;
        [SerializeField] string objectiveOnFinish;

        public UnityEvent onFinished = new UnityEvent();

        string[] current;
        int index = -1;
        int openedFrame;
        int closedFrame = -1;
        PlayerInputReader input;
        MonoBehaviour[] frozen;

        public string Prompt => prompt;
        // Not on the frame it closed, or the same E press would reopen it.
        public bool CanInteract => index < 0 && Time.frameCount != closedFrame;

        bool Seen => !string.IsNullOrEmpty(persistentId) && GameManager.Instance.IsCleared(persistentId);

        /// <summary>Replaces the lines at runtime (e.g. the breaker puzzle writes its order into the log).</summary>
        public void SetLines(params string[] newLines) => lines = newLines;

        public void Interact(GameObject interactor)
        {
            current = Seen && repeatLines != null && repeatLines.Length > 0 ? repeatLines : lines;
            input = interactor.GetComponent<PlayerInputReader>();
            frozen = new MonoBehaviour[]
            {
                interactor.GetComponent<FirstPersonController>(),
                interactor.GetComponent<ArcPistol>(),
                interactor.GetComponent<PlayerInteraction>()
            };
            foreach (var behaviour in frozen) if (behaviour != null) behaviour.enabled = false;

            index = 0;
            openedFrame = Time.frameCount;
            Show();
        }

        void Update()
        {
            if (index < 0 || Time.frameCount == openedFrame) return;
            if (input.Interact.WasPressedThisFrame() || input.Fire.WasPressedThisFrame())
            {
                index++;
                if (index < current.Length) Show();
                else Finish();
            }
        }

        void Show()
        {
            bool last = index == current.Length - 1;
            HUDController.Instance.ShowDialogue(speaker, current[index], last ? "[E] Close" : "[E] Continue");
        }

        void Finish()
        {
            index = -1;
            closedFrame = Time.frameCount;
            HUDController.Instance.HideDialogue();
            foreach (var behaviour in frozen) if (behaviour != null) behaviour.enabled = true;

            bool firstTime = !Seen;
            if (!string.IsNullOrEmpty(persistentId)) GameManager.Instance.MarkCleared(persistentId);
            if (firstTime && !string.IsNullOrEmpty(objectiveOnFinish)) HUDController.Instance.SetObjective(objectiveOnFinish);
            onFinished.Invoke();
        }
    }
}
