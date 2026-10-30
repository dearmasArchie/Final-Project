using UnityEngine;
using UnityEngine.InputSystem;

namespace SignalLost
{
    /// <summary>
    /// Door keypad: opens the HUD code panel. Type digits (number row or numpad), Backspace to correct,
    /// Esc to cancel. The code is checked as soon as all digits are entered.
    /// Used by the armory (4 digits, maintenance bunker puzzle) and the Wing A breaker room (3 gauge readings).
    /// </summary>
    public class CodeKeypad : MonoBehaviour, IInteractable
    {
        public enum CodeSource { Armory, Gauges }

        public const string UnlockedId = "ArmoryUnlocked";
        public const string BreakerRoomUnlockedId = "BreakerRoomUnlocked";

        [SerializeField] Door door;
        [SerializeField] CodeSource source = CodeSource.Armory;
        [SerializeField] string unlockId = UnlockedId;
        [SerializeField] string title = "ARMORY ACCESS";
        [SerializeField] string prompt = "Enter Armory Code";

        string entry = "";
        bool open;
        float closeAt = -1f;
        int closedFrame = -1;
        MonoBehaviour[] frozen;

        bool Unlocked => GameManager.Instance.IsCleared(unlockId);

        string Code
        {
            get
            {
                if (source == CodeSource.Armory) return GameManager.Instance.ArmoryPuzzle.Code;
                var readings = GameManager.Instance.GaugeReadings;
                return $"{readings[0]}{readings[1]}{readings[2]}";
            }
        }

        public string Prompt => Unlocked ? null : prompt;
        public bool CanInteract => !Unlocked && !open && Time.frameCount != closedFrame;

        public void Interact(GameObject interactor)
        {
            open = true;
            entry = "";
            frozen = new MonoBehaviour[]
            {
                interactor.GetComponent<FirstPersonController>(),
                interactor.GetComponent<ArcPistol>(),
                interactor.GetComponent<PlayerInteraction>()
            };
            foreach (var behaviour in frozen) if (behaviour != null) behaviour.enabled = false;
            MenuController.InputCaptured = true;
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnText;
            Refresh("", Color.white);
        }

        void OnText(char c)
        {
            string code = Code;
            if (!open || closeAt >= 0f || !char.IsDigit(c) || entry.Length >= code.Length) return;
            entry += c;
            Sfx.Play2D("keypad_key");
            if (entry.Length < code.Length)
            {
                Refresh("", Color.white);
                return;
            }

            if (entry == code)
            {
                GameManager.Instance.MarkCleared(unlockId);
                door.Unlock();
                door.Open();
                Sfx.Play2D("keypad_ok");
                Refresh("ACCESS GRANTED", new Color(0.4f, 1f, 0.5f));
                closeAt = Time.unscaledTime + 1f;
                ObjectiveManager.RefreshNow();
            }
            else
            {
                Sfx.Play2D("keypad_bad");
                Refresh("ACCESS DENIED", new Color(1f, 0.3f, 0.25f));
                entry = "";
            }
        }

        void Update()
        {
            if (!open) return;
            if (closeAt >= 0f)
            {
                if (Time.unscaledTime >= closeAt) Close();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Close();
            else if (keyboard.backspaceKey.wasPressedThisFrame && entry.Length > 0)
            {
                entry = entry.Substring(0, entry.Length - 1);
                Refresh("", Color.white);
            }
        }

        void Refresh(string status, Color statusColor)
        {
            var display = "";
            for (int i = 0; i < Code.Length; i++) display += i < entry.Length ? $"{entry[i]} " : "_ ";
            HUDController.Instance.ShowCodePanel(title, display.TrimEnd(), status, statusColor);
        }

        void Close()
        {
            open = false;
            closeAt = -1f;
            closedFrame = Time.frameCount;
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText;
            HUDController.Instance.HideCodePanel();
            foreach (var behaviour in frozen) if (behaviour != null) behaviour.enabled = true;
            MenuController.InputCaptured = false;
            MenuController.SuppressPauseFrame = Time.frameCount;
        }

        void OnDisable()
        {
            if (open && Keyboard.current != null) Keyboard.current.onTextInput -= OnText;
            if (open) MenuController.InputCaptured = false;
        }
    }
}
