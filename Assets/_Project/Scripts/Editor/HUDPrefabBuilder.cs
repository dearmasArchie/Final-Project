using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Builds the greybox HUD prefab (Assets/_Project/UI/HUD.prefab) with every element wired to HUDController.
    /// Re-running overwrites the prefab, so restyle it by editing the prefab once the layout is final.
    /// </summary>
    public static class HUDPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/UI/HUD.prefab";

        static readonly Color BarBackground = new Color(0f, 0f, 0f, 0.5f);
        static readonly Color HealthColor = new Color(0.85f, 0.15f, 0.15f);
        static readonly Color StaminaColor = new Color(0.95f, 0.8f, 0.2f);
        static readonly Color OverloadColor = new Color(0.3f, 0.9f, 1f);

        [MenuItem("Tools/Signal Lost/Build HUD Prefab")]
        public static GameObject Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("TextMeshPro resources missing",
                    "Import them first: Window > TextMeshPro > Import TMP Essential Resources. Then run this again.", "OK");
                return null;
            }

            var root = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(HUDController));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Top-left: health and stamina
            var healthFill = Bar(root.transform, "HealthBar", Anchor.TopLeft, new Vector2(40, -40), new Vector2(400, 24), HealthColor);
            var healthText = Text(healthFill.parent, "HealthText", Vector2.zero, new Vector2(400, 24), 18,
                TextAlignmentOptions.Center, Anchor.Stretch, "100 / 100");
            var staminaFill = Bar(root.transform, "StaminaBar", Anchor.TopLeft, new Vector2(40, -72), new Vector2(400, 10), StaminaColor);

            // Top-center: purge timer
            var timerText = Text(root.transform, "TimerText", new Vector2(0, -30), new Vector2(300, 60), 48,
                TextAlignmentOptions.Top, Anchor.TopCenter, "20:00");

            // Top-right: objective and fuses
            var objectiveText = Text(root.transform, "ObjectiveText", new Vector2(-40, -40), new Vector2(600, 40), 26,
                TextAlignmentOptions.TopRight, Anchor.TopRight, "Reach the freight elevator");
            var fuseText = Text(root.transform, "FuseText", new Vector2(-40, -80), new Vector2(600, 36), 22,
                TextAlignmentOptions.TopRight, Anchor.TopRight, "Override Fuses: 0 / 3");

            // Bottom-right: ammo
            var ammoText = Text(root.transform, "AmmoText", new Vector2(-40, 40), new Vector2(300, 60), 44,
                TextAlignmentOptions.BottomRight, Anchor.BottomRight, "12 / 48");

            // Bottom-right: Overload Beam charge, above the ammo
            var overloadFill = Bar(root.transform, "OverloadBar", Anchor.BottomRight, new Vector2(-40, 108), new Vector2(300, 8), OverloadColor);
            var overloadLabel = Text(root.transform, "OverloadLabel", new Vector2(-40, 118), new Vector2(400, 26), 16,
                TextAlignmentOptions.BottomRight, Anchor.BottomRight, "OVERLOAD");

            // Top-center under the timer: event line (lockdown countdown, upload progress)
            var eventText = Text(root.transform, "EventText", new Vector2(0, -92), new Vector2(800, 40), 28,
                TextAlignmentOptions.Top, Anchor.TopCenter, "LOCKDOWN 45s");
            eventText.color = new Color(1f, 0.6f, 0.2f);

            // Center: crosshair and interaction prompt
            var crosshair = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            crosshair.transform.SetParent(root.transform, false);
            SetRect(crosshair.GetComponent<RectTransform>(), Anchor.Center, Vector2.zero, new Vector2(6, 6));
            var promptText = Text(root.transform, "PromptText", new Vector2(0, -60), new Vector2(800, 40), 26,
                TextAlignmentOptions.Center, Anchor.Center, "[E] Interact");

            // Under the timer: progress bar for the signal upload and the boss's health
            var eventBarFill = Bar(root.transform, "EventBar", Anchor.TopCenter, new Vector2(0, -150), new Vector2(560, 16), new Color(0.3f, 0.9f, 1f));
            var eventBar = eventBarFill.parent.gameObject;
            var eventBarLabel = Text(eventBar.transform, "Label", new Vector2(0, 30), new Vector2(700, 30), 22,
                TextAlignmentOptions.Bottom, Anchor.TopCenter, "SIGNAL UPLOAD 0%");

            // Bottom-left: radio subtitles
            var radio = new GameObject("RadioPanel", typeof(RectTransform), typeof(Image));
            radio.transform.SetParent(root.transform, false);
            radio.GetComponent<Image>().color = new Color(0f, 0.05f, 0.08f, 0.75f);
            SetRect(radio.GetComponent<RectTransform>(), Anchor.BottomLeft, new Vector2(40, 40), new Vector2(620, 120));
            var radioSpeaker = Text(radio.transform, "Speaker", new Vector2(18, -10), new Vector2(580, 28), 20,
                TextAlignmentOptions.TopLeft, Anchor.TopLeft, "DR. REYES");
            radioSpeaker.color = new Color(0.4f, 0.9f, 1f);
            var radioText = Text(radio.transform, "Text", new Vector2(18, -40), new Vector2(584, 76), 20,
                TextAlignmentOptions.TopLeft, Anchor.TopLeft, "...");

            // Center: short messages such as "CHECKPOINT REACHED"
            var toastText = Text(root.transform, "ToastText", new Vector2(0, 160), new Vector2(900, 50), 36,
                TextAlignmentOptions.Center, Anchor.Center, "CHECKPOINT REACHED");
            toastText.color = new Color(0.5f, 1f, 0.6f);

            // Center: keypad code entry
            var code = new GameObject("CodePanel", typeof(RectTransform), typeof(Image));
            code.transform.SetParent(root.transform, false);
            code.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.05f, 0.92f);
            SetRect(code.GetComponent<RectTransform>(), Anchor.Center, new Vector2(0, 40), new Vector2(520, 300));
            var codeTitle = Text(code.transform, "Title", new Vector2(0, -20), new Vector2(480, 40), 26,
                TextAlignmentOptions.Top, Anchor.TopCenter, "ARMORY ACCESS");
            codeTitle.color = new Color(0.4f, 0.9f, 1f);
            var codeDisplay = Text(code.transform, "Display", new Vector2(0, 10), new Vector2(480, 90), 72,
                TextAlignmentOptions.Center, Anchor.Center, "_ _ _ _");
            var codeStatus = Text(code.transform, "Status", new Vector2(0, -70), new Vector2(480, 40), 28,
                TextAlignmentOptions.Center, Anchor.Center, "");
            Text(code.transform, "Hint", new Vector2(0, 16), new Vector2(480, 30), 18,
                TextAlignmentOptions.Bottom, Anchor.BottomCenter, "Type the code   [Backspace] Delete   [Esc] Cancel");

            var hazardText = Text(root.transform, "HazardText", new Vector2(0, -110), new Vector2(800, 40), 30,
                TextAlignmentOptions.Center, Anchor.Center, "!! HAZARD !!");
            hazardText.color = new Color(1f, 0.25f, 0.2f);

            // Bottom-center: dialogue box
            var panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            SetRect(panel.GetComponent<RectTransform>(), Anchor.BottomCenter, new Vector2(0, 60), new Vector2(1100, 220));
            var speaker = Text(panel.transform, "Speaker", new Vector2(30, -20), new Vector2(1040, 36), 26,
                TextAlignmentOptions.TopLeft, Anchor.TopLeft, "Dr. Reyes");
            speaker.color = new Color(0.4f, 0.9f, 1f);
            var body = Text(panel.transform, "Body", new Vector2(30, -62), new Vector2(1040, 120), 24,
                TextAlignmentOptions.TopLeft, Anchor.TopLeft, "...");
            var hint = Text(panel.transform, "Hint", new Vector2(-30, 16), new Vector2(400, 30), 18,
                TextAlignmentOptions.BottomRight, Anchor.BottomRight, "[E] Continue");

            var so = new SerializedObject(root.GetComponent<HUDController>());
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.FindProperty("staminaFill").objectReferenceValue = staminaFill;
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            so.FindProperty("fuseText").objectReferenceValue = fuseText;
            so.FindProperty("ammoText").objectReferenceValue = ammoText;
            so.FindProperty("promptText").objectReferenceValue = promptText;
            so.FindProperty("overloadFill").objectReferenceValue = overloadFill;
            so.FindProperty("overloadLabel").objectReferenceValue = overloadLabel;
            so.FindProperty("hazardText").objectReferenceValue = hazardText;
            so.FindProperty("eventText").objectReferenceValue = eventText;
            so.FindProperty("dialoguePanel").objectReferenceValue = panel;
            so.FindProperty("dialogueSpeaker").objectReferenceValue = speaker;
            so.FindProperty("dialogueBody").objectReferenceValue = body;
            so.FindProperty("dialogueHint").objectReferenceValue = hint;
            so.FindProperty("eventBar").objectReferenceValue = eventBar;
            so.FindProperty("eventBarFill").objectReferenceValue = eventBarFill;
            so.FindProperty("eventBarLabel").objectReferenceValue = eventBarLabel;
            so.FindProperty("radioPanel").objectReferenceValue = radio;
            so.FindProperty("radioSpeaker").objectReferenceValue = radioSpeaker;
            so.FindProperty("radioText").objectReferenceValue = radioText;
            so.FindProperty("toastText").objectReferenceValue = toastText;
            so.FindProperty("codePanel").objectReferenceValue = code;
            so.FindProperty("codeTitle").objectReferenceValue = codeTitle;
            so.FindProperty("codeDisplay").objectReferenceValue = codeDisplay;
            so.FindProperty("codeStatus").objectReferenceValue = codeStatus;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[Signal Lost] HUD prefab saved to {PrefabPath}");
            return prefab;
        }

        enum Anchor { TopLeft, TopCenter, TopRight, BottomLeft, BottomRight, BottomCenter, Center, Stretch }

        /// <summary>Background bar with a fill child; returns the fill (its anchorMax.x is the value).</summary>
        static RectTransform Bar(Transform parent, string name, Anchor anchor, Vector2 position, Vector2 size, Color fillColor)
        {
            var bg = new GameObject(name, typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(parent, false);
            bg.GetComponent<Image>().color = BarBackground;
            SetRect(bg.GetComponent<RectTransform>(), anchor, position, size);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bg.transform, false);
            fill.GetComponent<Image>().color = fillColor;
            var rect = fill.GetComponent<RectTransform>();
            SetRect(rect, Anchor.Stretch, Vector2.zero, Vector2.zero);
            return rect;
        }

        static TMP_Text Text(Transform parent, string name, Vector2 position, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Anchor anchor, string placeholder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = placeholder;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            SetRect(go.GetComponent<RectTransform>(), anchor, position, size);
            return text;
        }

        static void SetRect(RectTransform rect, Anchor anchor, Vector2 position, Vector2 size)
        {
            Vector2 a;
            switch (anchor)
            {
                case Anchor.TopLeft: a = new Vector2(0, 1); break;
                case Anchor.TopCenter: a = new Vector2(0.5f, 1); break;
                case Anchor.TopRight: a = new Vector2(1, 1); break;
                case Anchor.BottomLeft: a = new Vector2(0, 0); break;
                case Anchor.BottomRight: a = new Vector2(1, 0); break;
                case Anchor.BottomCenter: a = new Vector2(0.5f, 0); break;
                case Anchor.Center: a = new Vector2(0.5f, 0.5f); break;
                default:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    return;
            }
            rect.anchorMin = rect.anchorMax = rect.pivot = a;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
