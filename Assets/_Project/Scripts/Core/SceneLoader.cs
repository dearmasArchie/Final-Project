using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SignalLost
{
    /// <summary>
    /// Loads scenes with a fade to black. After loading, moves the player to the SpawnPoint
    /// named in GameManager.NextSpawnPointId. Created automatically, like GameManager.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        const float FadeDuration = 0.4f;

        public static SceneLoader Instance { get; private set; }

        /// <summary>
        /// Raised just before the current scene unloads. The player saves HP and ammo here
        /// (GameManager.SavePlayerState) so they carry over.
        /// </summary>
        public static event Action BeforeSceneUnload;

        public bool IsLoading { get; private set; }

        CanvasGroup fadeGroup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject(nameof(SceneLoader)).AddComponent<SceneLoader>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeOverlay();
        }

        /// <param name="savePlayer">False when the player's state was already set (checkpoint restore, new run).</param>
        public void Load(string sceneName, bool savePlayer = true)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName, savePlayer));
        }

        IEnumerator LoadRoutine(string sceneName, bool savePlayer)
        {
            IsLoading = true;
            yield return Fade(0f, 1f);

            if (savePlayer) BeforeSceneUnload?.Invoke();
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;

            // All objects in the new scene have run Awake/OnEnable, so SpawnPoints are registered.
            SpawnPoint.PlacePlayer(GameManager.Instance.NextSpawnPointId);

            yield return Fade(1f, 0f);
            IsLoading = false;
        }

        IEnumerator Fade(float from, float to)
        {
            fadeGroup.blocksRaycasts = true;
            for (float t = 0f; t < FadeDuration; t += Time.unscaledDeltaTime)
            {
                fadeGroup.alpha = Mathf.Lerp(from, to, t / FadeDuration);
                yield return null;
            }
            fadeGroup.alpha = to;
            fadeGroup.blocksRaycasts = to > 0f;
        }

        void CreateFadeOverlay()
        {
            var canvasGo = new GameObject("FadeCanvas", typeof(Canvas), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // above the HUD and menus

            fadeGroup = canvasGo.GetComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            var imageGo = new GameObject("Black", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = imageGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            imageGo.GetComponent<Image>().color = Color.black;
        }
    }
}
