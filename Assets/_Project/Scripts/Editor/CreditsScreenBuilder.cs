using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Replaces the Main Menu's placeholder credits label with a scrollable list filled from Resources/Credits.txt.
    /// Also called by the Phase 3 builder when it rebuilds the menu. Bridge command: credits (re-runs art-ui afterwards).
    /// </summary>
    public static class CreditsScreenBuilder
    {
        const string MainMenuScene = "Assets/_Project/Scenes/MainMenu.unity";

        [MenuItem("Tools/Signal Lost/Build Credits Screen")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScene);
            RectTransform panel = null;
            foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
                if (t.name == "CreditsPanel") panel = t;
            if (panel == null) { EditorCommandBridge.Log("credits: CreditsPanel not found"); return; }
            Populate(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            UIArtBuilder.Build(); // restyle the new pieces (font, colors, frame around the scroll area)
            EditorCommandBridge.Log("credits: scrollable credits panel built from Resources/Credits.txt");
        }

        /// <summary>Builds (or rebuilds) the scroll area inside a CreditsPanel. The panel's Title child stays.</summary>
        public static void Populate(RectTransform panel)
        {
            foreach (var name in new[] { "Text", "CreditsScroll", "ScrollHint" })
            {
                var old = panel.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var scrollGo = new GameObject("CreditsScroll", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);
            var view = (RectTransform)scrollGo.transform;
            view.anchorMin = view.anchorMax = new Vector2(0.5f, 0.5f);
            view.sizeDelta = new Vector2(1300f, 480f);
            view.anchoredPosition = new Vector2(0f, 20f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CreditsText), typeof(ContentSizeFitter));
            textGo.transform.SetParent(view, false);
            var content = (RectTransform)textGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(30f, 0f);
            content.offsetMax = new Vector2(-30f, 0f);
            content.anchoredPosition = Vector2.zero;
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = 6f;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Resources/Credits.txt").text;
            textGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = view;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var hintGo = new GameObject("ScrollHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintGo.transform.SetParent(panel, false);
            var hint = hintGo.GetComponent<TextMeshProUGUI>();
            hint.text = "Scroll with the mouse wheel";
            hint.fontSize = 18f;
            hint.alignment = TextAlignmentOptions.Center;
            hint.raycastTarget = false;
            var hintRect = (RectTransform)hintGo.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.sizeDelta = new Vector2(600f, 30f);
            hintRect.anchoredPosition = new Vector2(0f, -240f);
        }
    }
}
