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

        public void Play()
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
