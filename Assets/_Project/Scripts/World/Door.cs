using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Sliding door opened with E (on the panel or a keypad child). Can be locked with a reason,
    /// and opened, closed, locked, or unlocked by other scripts (briefing, valve, lockdown).
    /// </summary>
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform panel;
        [SerializeField] Vector3 openOffset = new Vector3(0f, 3.3f, 0f);
        [SerializeField] float openSpeed = 4f;
        [SerializeField] bool startsLocked;
        [SerializeField] string lockedReason = "Locked";
        [Tooltip("If set, the door starts unlocked once this ID is cleared in GameManager (e.g. after the briefing).")]
        [SerializeField] string unlockedWhenCleared;

        Vector3 closedPosition;

        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }

        public string Prompt => IsOpen ? null : IsLocked ? lockedReason : "Open Door";
        public bool CanInteract => !IsOpen && !IsLocked;

        void Awake()
        {
            closedPosition = panel.localPosition;
            IsLocked = startsLocked;
        }

        void Start()
        {
            if (!string.IsNullOrEmpty(unlockedWhenCleared) && GameManager.Instance.IsCleared(unlockedWhenCleared))
                IsLocked = false;
        }

        void Update()
        {
            Vector3 target = IsOpen ? closedPosition + openOffset : closedPosition;
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, openSpeed * Time.deltaTime);
        }

        public void Interact(GameObject interactor) => Open();

        public void Open()
        {
            if (!IsOpen && Time.timeSinceLevelLoad > 0.5f) Sfx.Play("door_open", panel.position);
            IsOpen = true;
        }

        public void Close()
        {
            if (IsOpen && Time.timeSinceLevelLoad > 0.5f) Sfx.Play("door_close", panel.position);
            IsOpen = false;
        }
        public void Unlock() => IsLocked = false;

        public void Lock(string reason)
        {
            lockedReason = reason;
            IsLocked = true;
        }
    }
}
