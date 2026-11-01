using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SignalLost
{
    /// <summary>
    /// Story introduction shown between pressing Play and the start of the game: a few black pages of text that type in.
    /// Any key or click moves on (the first press finishes the page), Esc skips it. Built at runtime so the menu scene needs no setup.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        static readonly string[] Pages =
        {
            "OUTPOST 14\n<size=60%>An isolated island facility researching energy weapons.\nYou arrived on the morning supply run.</size>",
            "Minutes after you land, lightning strikes.\n<size=60%>OVERSEER, the facility's security AI, is corrupted.\nEveryone on the island is now an intruder.</size>",
            "Security drones and guard robots turn hostile.\n<size=60%>The radio tower goes dark. The supply boat leaves without you.</size>",
            "OVERSEER starts a 20-minute <color=#FF5A4A>Purge Protocol</color>.\n<size=60%>When the timer ends, the defense grid fires on everything still alive.</size>",
            "Find Dr. Reyes. Restore the signal.\n<size=60%>Get off the island.</size>",
        };

        const float CharsPerSecond = 38f;
        const float AutoAdvanceSeconds = 4.5f;

        System.Action onFinished;
        CanvasGroup group;
        TMP_Text body;
        TMP_Text hint;
        bool skipAll;

        /// <summary>Shows the intro on top of everything and calls onFinished when it ends. Font is borrowed from the menu text.</summary>
        public static void Show(TMP_FontAsset font, System.Action onFinished)
        {
            var go = new GameObject("IntroSequence", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var intro = go.AddComponent<IntroSequence>();
            intro.onFinished = onFinished;
            intro.group = go.GetComponent<CanvasGroup>();
            intro.group.alpha = 0f;

            var bg = new GameObject("Black", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = Color.black;

            intro.body = MakeText(go.transform, "Body", font, 54, TextAlignmentOptions.Center, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f));
            intro.hint = MakeText(go.transform, "Hint", font, 24, TextAlignmentOptions.Center, new Vector2(0.2f, 0.05f), new Vector2(0.8f, 0.12f));
            intro.hint.color = new Color(1f, 1f, 1f, 0.45f);
            intro.hint.text = "Press any key to continue     Esc to skip";
        }

        static TMP_Text MakeText(Transform parent, string name, TMP_FontAsset font, float size, TextAlignmentOptions align, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = new Color(0.9f, 0.92f, 0.95f);
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void Start() => StartCoroutine(Run());

        IEnumerator Run()
        {
            yield return FadeGroup(0f, 1f, 0.5f);
            foreach (var page in Pages)
            {
                if (skipAll) break;
                body.text = page;
                body.maxVisibleCharacters = 0;
                body.ForceMeshUpdate();
                int total = body.textInfo.characterCount;

                float shown = 0f;
                while (shown < total && !skipAll)
                {
                    shown += CharsPerSecond * Time.unscaledDeltaTime;
                    body.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                    if (PressedAnything()) shown = total;
                    yield return null;
                }
                body.maxVisibleCharacters = total;

                yield return null; // let the same key press that finished the typing pass
                float wait = 0f;
                while (wait < AutoAdvanceSeconds && !skipAll && !PressedAnything())
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return FadeBody(1f, 0f, 0.35f);
                body.alpha = 1f;
            }
            hint.text = "";
            yield return FadeGroup(1f, 0f, 0.4f);
            onFinished?.Invoke();
            Destroy(gameObject);
        }

        bool PressedAnything()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) { skipAll = true; return true; }
            if (k != null && k.anyKey.wasPressedThisFrame) return true;
            return m != null && m.leftButton.wasPressedThisFrame;
        }

        IEnumerator FadeGroup(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            group.alpha = to;
        }

        IEnumerator FadeBody(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                body.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            body.alpha = to;
        }
    }
}
