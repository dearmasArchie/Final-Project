using TMPro;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// The in-game HUD. Reads the timer and fuses from GameManager by itself.
    /// Player scripts push HP, stamina, ammo, and the interaction prompt through the Set methods.
    /// Build or rebuild the prefab with Tools > Signal Lost > Build HUD Prefab.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [SerializeField] RectTransform healthFill;
        [SerializeField] TMP_Text healthText;
        [SerializeField] RectTransform staminaFill;
        [SerializeField] TMP_Text timerText;
        [SerializeField] TMP_Text objectiveText;
        [SerializeField] TMP_Text fuseText;
        [SerializeField] TMP_Text ammoText;
        [SerializeField] TMP_Text promptText;
        [SerializeField] RectTransform overloadFill;
        [SerializeField] TMP_Text overloadLabel;
        [SerializeField] TMP_Text hazardText;
        [SerializeField] TMP_Text eventText;
        [SerializeField] GameObject dialoguePanel;
        [SerializeField] TMP_Text dialogueSpeaker;
        [SerializeField] TMP_Text dialogueBody;
        [SerializeField] TMP_Text dialogueHint;
        [SerializeField] Color timerNormal = Color.white;
        [SerializeField] Color timerWarning = new Color(1f, 0.25f, 0.2f);

        const float TimerWarningSeconds = 120f;

        void Awake() => Instance = this;

        void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.FusesChanged += UpdateFuses;
        }

        void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.FusesChanged -= UpdateFuses;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            UpdateFuses(GameManager.Instance.FusesCollected);
            SetPrompt(null);
            SetHazardWarning(null);
            SetEventText(null);
            HideDialogue();
            SetOverload(0f, false);
        }

        void Update()
        {
            float seconds = GameManager.Instance.TimeRemaining;
            int minutes = Mathf.FloorToInt(seconds / 60f);
            timerText.text = $"{minutes:00}:{Mathf.FloorToInt(seconds % 60f):00}";
            timerText.color = seconds <= TimerWarningSeconds ? timerWarning : timerNormal;
        }

        public void SetHealth(float current, float max)
        {
            SetFill(healthFill, max <= 0f ? 0f : current / max);
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        /// <summary>0 = empty, 1 = full.</summary>
        public void SetStamina(float normalized) => SetFill(staminaFill, normalized);

        public void SetAmmo(int magazine, int reserve) => ammoText.text = $"{magazine} / {reserve}";

        public void SetObjective(string text) => objectiveText.text = text;

        /// <summary>
        /// Pass null or empty to hide the prompt. "[E] " is added when usable;
        /// locked objects (usable = false) show their reason without the key.
        /// </summary>
        public void SetPrompt(string text, bool usable = true)
        {
            bool visible = !string.IsNullOrEmpty(text);
            promptText.gameObject.SetActive(visible);
            if (visible) promptText.text = usable ? $"[E] {text}" : text;
        }

        /// <summary>Overload Beam charge (0–1) and whether it can be charged right now (ammo, cooldown).</summary>
        public void SetOverload(float charge01, bool ready)
        {
            SetFill(overloadFill, charge01);
            overloadLabel.text = charge01 >= 1f ? "OVERLOAD READY – RELEASE" : ready ? "OVERLOAD" : "OVERLOAD (cooling)";
            overloadLabel.alpha = ready ? 1f : 0.4f;
        }

        /// <summary>Red warning under the crosshair, e.g. electrified water. Null hides it.</summary>
        public void SetHazardWarning(string text) => SetOptional(hazardText, text);

        /// <summary>Top-center event line under the timer, e.g. the lockdown countdown. Null hides it.</summary>
        public void SetEventText(string text) => SetOptional(eventText, text);

        public void ShowDialogue(string speaker, string body, string hint)
        {
            dialoguePanel.SetActive(true);
            dialogueSpeaker.text = speaker;
            dialogueBody.text = body;
            dialogueHint.text = hint;
        }

        public void HideDialogue() => dialoguePanel.SetActive(false);

        static void SetOptional(TMP_Text label, string text)
        {
            bool visible = !string.IsNullOrEmpty(text);
            label.gameObject.SetActive(visible);
            if (visible) label.text = text;
        }

        void UpdateFuses(int collected) => fuseText.text = $"Override Fuses: {collected} / {GameManager.FuseCount}";

        static void SetFill(RectTransform fill, float normalized)
        {
            var max = fill.anchorMax;
            max.x = Mathf.Clamp01(normalized);
            fill.anchorMax = max;
        }
    }
}
