using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SignalLost
{
    /// <summary>
    /// In-game menus: Pause (Esc), Game Over (death or purge timer), Victory, and Settings.
    /// Built by Tools > Signal Lost (the "GameMenus" prefab); buttons call the public methods.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        const string FirstLevel = "IslandExterior";
        const string MainMenuScene = "MainMenu";

        public static MenuController Instance { get; private set; }

        /// <summary>True while another panel (the code keypad) is reading the keyboard; Esc then doesn't pause.</summary>
        public static bool InputCaptured { get; set; }
        /// <summary>Frame on which that panel closed with Esc, so the same press doesn't also open the pause menu.</summary>
        public static int SuppressPauseFrame { get; set; } = -1;

        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] GameObject victoryPanel;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] TMP_Text gameOverReason;
        [SerializeField] TMP_Text victoryStats;
        [SerializeField] TMP_Text victoryRank;
        [SerializeField] Button[] retryButtons;

        InputAction pauseAction;
        GameObject settingsReturnPanel;
        bool runEnded;

        void Awake()
        {
            Instance = this;
            pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");
        }

        void OnEnable() => pauseAction.Enable();

        void OnDisable()
        {
            pauseAction.Disable();
            if (GameManager.Instance != null) GameManager.Instance.TimeUp -= OnTimeUp;
        }

        void OnDestroy()
        {
            pauseAction.Dispose();
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            HideAll();
            GameManager.Instance.TimeUp += OnTimeUp;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) player.GetComponent<PlayerHealth>().Died += OnPlayerDied;
        }

        void Update()
        {
            if (!pauseAction.WasPressedThisFrame() || runEnded) return;
            if (InputCaptured || Time.frameCount == SuppressPauseFrame) return;
            if (settingsPanel.activeSelf) CloseSettings();
            else if (pausePanel.activeSelf) Resume();
            else Pause();
        }

        // ------------------------------------------------------------------ screens

        public void Pause()
        {
            Show(pausePanel);
            SetPaused(true);
        }

        public void Resume()
        {
            HideAll();
            SetPaused(false);
        }

        void OnPlayerDied() => ShowGameOver("You were killed in action.");

        void OnTimeUp() => ShowGameOver("PURGE PROTOCOL EXECUTED\nThe defense grid fired on the whole island.");

        public void ShowGameOver(string reason)
        {
            if (runEnded) return;
            runEnded = true;
            gameOverReason.text = reason;
            Show(gameOverPanel);
            SetPaused(true);
        }

        public void ShowVictory()
        {
            runEnded = true;
            var gm = GameManager.Instance;
            victoryStats.text =
                $"Completion time   {FormatTime(gm.ElapsedTime)}\n" +
                $"Time remaining    {FormatTime(gm.TimeRemaining)}\n" +
                $"Enemies destroyed {gm.EnemiesDestroyed}\n" +
                $"Accuracy          {Mathf.RoundToInt(gm.Accuracy * 100f)}%";
            if (victoryRank != null) victoryRank.text = $"RANK  {Rank(gm.TimeRemaining / GameManager.PurgeDuration, gm.Accuracy)}";
            Show(victoryPanel);
            SetPaused(true);
            MusicManager.PlayVictory();
        }

        public void OpenSettings()
        {
            settingsReturnPanel = pausePanel.activeSelf ? pausePanel : null;
            Show(settingsPanel);
        }

        public void CloseSettings()
        {
            if (settingsReturnPanel != null) Show(settingsReturnPanel);
            else Resume();
        }

        // ------------------------------------------------------------------ buttons

        public void RetryFromCheckpoint()
        {
            if (!GameManager.Instance.HasCheckpoint)
            {
                RestartGame();
                return;
            }
            LeaveMenus();
            GameManager.Instance.RestoreCheckpoint();
        }

        public void RestartGame()
        {
            LeaveMenus();
            GameManager.Instance.ResetRun();
            SceneLoader.Instance.Load(FirstLevel, savePlayer: false);
        }

        public void QuitToMainMenu()
        {
            LeaveMenus();
            GameManager.Instance.ResetRun();
            SceneLoader.Instance.Load(MainMenuScene, savePlayer: false);
        }

        // ------------------------------------------------------------------ helpers

        void Show(GameObject panel)
        {
            HideAll();
            panel.SetActive(true);
            bool canRetry = GameManager.Instance.HasCheckpoint;
            foreach (var button in retryButtons)
                button.GetComponentInChildren<TMP_Text>().text = canRetry ? "Retry from Checkpoint" : "Retry from Start";
        }

        void HideAll()
        {
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);
            victoryPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        static void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            FirstPersonController.SetCursorLocked(!paused);
        }

        /// <summary>S, A, B or C from how much of the purge timer was left and how accurate the shooting was.</summary>
        public static string Rank(float timeLeft01, float accuracy)
        {
            float score = Mathf.Clamp01(timeLeft01) * 50f + Mathf.Clamp01(accuracy) * 50f;
            return score >= 70f ? "S" : score >= 55f ? "A" : score >= 40f ? "B" : "C";
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void LeaveMenus()
        {
            MusicManager.StopVictory();
            HideAll();
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        static string FormatTime(float seconds) =>
            $"{Mathf.FloorToInt(seconds / 60f):00}:{Mathf.FloorToInt(seconds % 60f):00}";
    }
}
