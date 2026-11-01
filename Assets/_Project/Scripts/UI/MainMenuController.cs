using UnityEngine;

namespace SignalLost
{
    /// <summary>Main menu: Play, Controls, Settings, Credits, Quit.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject controlsPanel;
        [SerializeField] GameObject settingsPanel;
        [SerializeField] GameObject creditsPanel;

        void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            FirstPersonController.SetCursorLocked(false);
            Back();
        }

        bool introPlaying;

        /// <summary>Plays the story introduction, then starts the game. Retries and Play Again go straight to the island and skip it.</summary>
        public void Play()
        {
            if (introPlaying) return;
            introPlaying = true;
            var font = GetComponentInChildren<TMPro.TMP_Text>(true)?.font;
            IntroSequence.Show(font, StartGame);
        }

        public void PlayImmediate() => StartGame();

        void StartGame()
        {
            GameManager.Instance.ResetRun();
            SceneLoader.Instance.Load("IslandExterior", savePlayer: false);
        }

        public void ShowControls() => Show(controlsPanel);
        public void ShowSettings() => Show(settingsPanel);
        public void ShowCredits() => Show(creditsPanel);
        public void Back() => Show(mainPanel);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Show(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            controlsPanel.SetActive(panel == controlsPanel);
            settingsPanel.SetActive(panel == settingsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
        }
    }
}
