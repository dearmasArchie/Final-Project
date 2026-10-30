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
        [SerializeField] GameObject eventBar;
        [SerializeField] RectTransform eventBarFill;
        [SerializeField] TMP_Text eventBarLabel;
        [SerializeField] GameObject radioPanel;
        [SerializeField] TMP_Text radioSpeaker;
        [SerializeField] TMP_Text radioText;
        [SerializeField] TMP_Text toastText;
        [SerializeField] GameObject codePanel;
        [SerializeField] TMP_Text codeTitle;
        [SerializeField] TMP_Text codeDisplay;
        [SerializeField] TMP_Text codeStatus;
        [SerializeField] TMP_Text hitMarker;
        [SerializeField] UnityEngine.UI.Image damageFlash;
        [SerializeField] Color timerNormal = Color.white;
        [SerializeField] Color timerWarning = new Color(1f, 0.25f, 0.2f);

        const float TimerWarningSeconds = 120f;

        float radioHideTime;
        float toastHideTime;
        float hitMarkerTime;
        float damageFlashAlpha;

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
            HideEventBar();
            radioPanel.SetActive(false);
            toastText.gameObject.SetActive(false);
            HideCodePanel();
            hitMarker.alpha = 0f;
            damageFlash.color = new Color(1f, 0f, 0f, 0f);
        }

        void Update()
        {
            float seconds = GameManager.Instance.TimeRemaining;
            int minutes = Mathf.FloorToInt(seconds / 60f);
            timerText.text = $"{minutes:00}:{Mathf.FloorToInt(seconds % 60f):00}";
            timerText.color = seconds <= TimerWarningSeconds ? timerWarning : timerNormal;

            if (radioPanel.activeSelf && Time.unscaledTime >= radioHideTime) radioPanel.SetActive(false);
            if (toastText.gameObject.activeSelf && Time.unscaledTime >= toastHideTime) toastText.gameObject.SetActive(false);

            hitMarker.alpha = Mathf.Clamp01((hitMarkerTime - Time.unscaledTime) / 0.15f);
            if (damageFlashAlpha > 0f)
            {
                damageFlashAlpha = Mathf.Max(0f, damageFlashAlpha - Time.unscaledDeltaTime * 1.5f);
                damageFlash.color = new Color(1f, 0f, 0f, damageFlashAlpha);
            }
        }

        /// <summary>X around the crosshair when a shot hits; red for a weak-point crit.</summary>
        public void ShowHitMarker(bool crit)
        {
            hitMarker.color = crit ? new Color(1f, 0.25f, 0.2f) : Color.white;
            hitMarker.fontSize = crit ? 44f : 34f;
            hitMarkerTime = Time.unscaledTime + 0.15f;
        }

        /// <summary>Red screen-edge flash when the player takes damage (stronger for bigger hits).</summary>
        public void FlashDamage(float amount) => damageFlashAlpha = Mathf.Clamp(damageFlashAlpha + amount / 60f, 0.15f, 0.6f);

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

        /// <summary>Progress bar under the timer: signal upload or boss health.</summary>
        public void SetEventBar(string label, float value01)
        {
            eventBar.SetActive(true);
            eventBarLabel.text = label;
            SetFill(eventBarFill, value01);
        }

        public void HideEventBar() => eventBar.SetActive(false);

        /// <summary>Radio subtitle (bottom-left) for a few seconds.</summary>
        public void ShowRadio(string speaker, string text, float seconds)
        {
            radioPanel.SetActive(true);
            radioSpeaker.text = speaker;
            radioText.text = text;
            radioHideTime = Time.unscaledTime + seconds;
        }

        /// <summary>Keypad code entry panel: title, digits so far (e.g. "4 7 _ _") and a status line.</summary>
        public void ShowCodePanel(string title, string display, string status, Color statusColor)
        {
            codePanel.SetActive(true);
            codeTitle.text = title;
            codeDisplay.text = display;
            codeStatus.text = status;
            codeStatus.color = statusColor;
        }

        public void HideCodePanel() => codePanel.SetActive(false);

        /// <summary>Short message in the middle of the screen, e.g. "CHECKPOINT REACHED".</summary>
        public void ShowToast(string text, float seconds = 2.5f)
        {
            toastText.gameObject.SetActive(true);
            toastText.text = text;
            toastHideTime = Time.unscaledTime + seconds;
        }

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
