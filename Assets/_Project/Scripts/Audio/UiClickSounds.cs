using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SignalLost
{
    /// <summary>Gives every Button in a freshly loaded scene (menus, pause, game over) the UI click sound.</summary>
    public static class UiClickSounds
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Hook();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Hook();

        static void Hook()
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                button.onClick.RemoveListener(Click); // safe to call twice
                button.onClick.AddListener(Click);
            }
        }

        static void Click() => Sfx.PlayUi("ui_click");
    }
}
