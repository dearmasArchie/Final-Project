using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 4 UI pass: restyles the HUD prefab, the GameMenus prefab and the Main Menu scene in place with the
    /// Kenney Fantasy UI Borders (CC0) and the Rajdhani font. Charcoal panels, off-white text, amber accents.
    /// Nothing is rebuilt, so all script wiring stays. Safe to re-run: its own additions are named "UIArt_*" and
    /// are replaced each time. HUDPrefabBuilder and Phase3ContentBuilder call it after rebuilding.
    /// Run with the <c>art-ui</c> bridge command.
    /// </summary>
    public static class UIArtBuilder
    {
        const string BorderDir = "Assets/_Project/Art/kenney_fantasy-ui-borders/PNG/Default/";
        const string FontDir = "Assets/_Project/UI/Fonts/";
        const string MenusPath = "Assets/_Project/UI/GameMenus.prefab";
        const string MainMenuScene = "Assets/_Project/Scenes/MainMenu.unity";
        const string Prefix = "UIArt_";
        const float OriginalTitleY = 330f; // where Phase3ContentBuilder puts menu titles

        static readonly Color Ink = new Color(0.93f, 0.92f, 0.88f);
        static readonly Color Amber = new Color(1f, 0.72f, 0.25f);
        static readonly Color PanelDark = new Color(0.06f, 0.065f, 0.07f, 0.88f);
        static readonly Color FrameColor = new Color(0.85f, 0.83f, 0.78f, 0.55f);
        static readonly Color BarBack = new Color(0f, 0f, 0f, 0.55f);

        static Sprite frameSprite, lineSprite, fillSprite, dividerSprite;
        static TMP_FontAsset regular, bold;

        public static void Build()
        {
            string previous = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            Prepare();
            StyleHud(HUDPrefabBuilder.PrefabPath);
            StyleMenusPrefab();
            StyleMainMenuScene();
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != MainMenuScene) EditorSceneManager.OpenScene(previous);
            EditorCommandBridge.Log("art-ui: HUD, GameMenus and MainMenu restyled (Kenney borders + Rajdhani)");
        }

        // ------------------------------------------------------------------ assets

        static void Prepare()
        {
            frameSprite = SlicedSprite(BorderDir + "Border/panel-border-009.png", new Vector4(14, 14, 14, 14));
            lineSprite = SlicedSprite(BorderDir + "Border/panel-border-015.png", new Vector4(6, 6, 6, 6));
            fillSprite = SlicedSprite(BorderDir + "Panel/panel-015.png", new Vector4(6, 6, 6, 6));
            dividerSprite = SlicedSprite(BorderDir + "Divider Fade/divider-fade-001.png", new Vector4(0, 0, 22, 0));
            regular = FontAsset("Rajdhani-SemiBold");
            bold = FontAsset("Rajdhani-Bold");
        }

        static Sprite SlicedSprite(string path, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>A dynamic TMP font asset next to the TTF (glyphs are added as text needs them).</summary>
        static TMP_FontAsset FontAsset(string name)
        {
            string path = FontDir + name + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + name + ".ttf");
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = name + " SDF";
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = name + " Atlas";
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?%/()[]-–_'\"+*#<>=&");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // ------------------------------------------------------------------ HUD

        public static void StyleHud(string prefabPath)
        {
            if (frameSprite == null) Prepare();
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            ClearOwn(root.transform);

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool heading = text.name is "TimerText" or "Speaker" or "Title" or "Display" or "ToastText";
                text.font = heading ? bold : regular;
                if (IsCyan(text.color) || text.name == "ToastText") text.color = Amber;
                else if (text.color == Color.white) text.color = Ink;
            }
            Find(root, "TimerText").GetComponent<TMP_Text>().characterSpacing = 4f;
            var timer = root.GetComponent<HUDController>();
            var so = new SerializedObject(timer);
            so.FindProperty("timerNormal").colorValue = Ink;
            so.ApplyModifiedPropertiesWithoutUndo();

            StyleBar(root, "HealthBar", new Color(0.78f, 0.16f, 0.14f));
            StyleBar(root, "StaminaBar", new Color(0.9f, 0.88f, 0.82f));
            StyleBar(root, "OverloadBar", new Color(0.75f, 0.85f, 0.9f));
            StyleBar(root, "EventBar", Amber);

            StylePanel(Find(root, "RadioPanel"), new Color(0.05f, 0.055f, 0.06f, 0.8f));
            StylePanel(Find(root, "CodePanel"), new Color(0.04f, 0.045f, 0.05f, 0.94f));
            StylePanel(Find(root, "DialoguePanel"), new Color(0.04f, 0.045f, 0.05f, 0.85f));

            // Dividers flanking the purge timer
            var timerRect = Find(root, "TimerText").GetComponent<RectTransform>();
            Dividers(root.transform, new Vector2(0.5f, 1f), timerRect.anchoredPosition.y - timerRect.sizeDelta.y / 2f + 6f, 95f, 200f, 0.7f);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void StyleBar(GameObject root, string name, Color fill)
        {
            var bar = Find(root, name);
            if (bar == null) return;
            var back = bar.GetComponent<Image>();
            back.sprite = null;
            back.color = BarBack;
            bar.transform.Find("Fill").GetComponent<Image>().color = fill;
            var frame = AddImage(bar.transform, "Frame", lineSprite, FrameColor, 2f);
            Stretch(frame.rectTransform, 3f);
        }

        static void StylePanel(GameObject panel, Color background)
        {
            if (panel == null) return;
            var image = panel.GetComponent<Image>();
            image.sprite = fillSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = background;
            var frame = AddImage(panel.transform, "Frame", frameSprite, FrameColor, 1f);
            frame.transform.SetAsFirstSibling();
            Stretch(frame.rectTransform, -4f);
        }

        // ------------------------------------------------------------------ menus

        static void StyleMenusPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(MenusPath);
            StyleMenuCanvas(root);
            PrefabUtility.SaveAsPrefabAsset(root, MenusPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void StyleMainMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScene);
            var canvas = GameObject.Find("MainMenu");
            if (canvas != null) StyleMenuCanvas(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void StyleMenuCanvas(GameObject canvas)
        {
            ClearOwn(canvas.transform);
            foreach (Transform child in canvas.transform)
            {
                var title = child.Find("Title");
                if (title != null) StyleMenuPanel((RectTransform)child, title.GetComponent<TMP_Text>());
            }
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != bold) text.font = regular;
                if (text.color == Color.white || IsCyan(text.color)) text.color = Ink;
            }
            foreach (var button in canvas.GetComponentsInChildren<Button>(true)) StyleButton(button);
            foreach (var slider in canvas.GetComponentsInChildren<Slider>(true)) StyleSlider(slider);
        }

        static void StyleMenuPanel(RectTransform panel, TMP_Text title)
        {
            title.font = bold;
            title.color = Ink;
            title.characterSpacing = 6f;

            // Pull the title down to sit just above the panel's content (the greybox left a big gap).
            var titleRect = title.rectTransform;
            float textHeight = title.GetPreferredValues(title.text, titleRect.sizeDelta.x, 0f).y;
            float contentTop = ContentBounds(panel, title.transform).yMax;
            titleRect.anchoredPosition = new Vector2(titleRect.anchoredPosition.x,
                Mathf.Min(OriginalTitleY, contentTop + 70f + textHeight / 2f));

            // Frame around everything centered in the panel (title, buttons, sliders, text), with padding.
            var bounds = ContentBounds(panel);
            var backing = AddImage(panel, "Backing", fillSprite, PanelDark, 1f);
            var frame = AddImage(panel, "Frame", frameSprite, FrameColor, 0.6f);
            foreach (var image in new[] { frame, backing }) // backing ends up first (drawn behind)
            {
                image.transform.SetAsFirstSibling();
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = bounds.center;
                rect.sizeDelta = bounds.size + new Vector2(120f, 90f);
            }

            // Fading dividers meeting under the title
            Dividers(panel, new Vector2(0.5f, 0.5f), titleRect.anchoredPosition.y - textHeight / 2f - 14f, 6f, 260f, 1f);
        }

        /// <summary>Union of the panel's centered children, using a label's real text width instead of its box.</summary>
        static Rect ContentBounds(RectTransform panel, Transform skip = null)
        {
            bool any = false;
            Vector2 min = Vector2.zero, max = Vector2.zero;
            foreach (RectTransform child in panel)
            {
                if (child == skip || child.name.StartsWith(Prefix) || child.anchorMin != child.anchorMax) continue;
                Vector2 size = child.sizeDelta;
                if (child.TryGetComponent<TMP_Text>(out var text))
                {
                    var preferred = text.GetPreferredValues(text.text, size.x, 0f);
                    size = new Vector2(Mathf.Min(size.x, preferred.x), Mathf.Min(size.y, preferred.y));
                    if (text.alignment == TextAlignmentOptions.Right) // right-aligned slider labels hug their right edge
                    {
                        Vector2 right = child.anchoredPosition + new Vector2(child.sizeDelta.x / 2f, 0f);
                        Grow(ref any, ref min, ref max, right - new Vector2(size.x, size.y / 2f), right + new Vector2(0f, size.y / 2f));
                        continue;
                    }
                }
                Grow(ref any, ref min, ref max, child.anchoredPosition - size / 2f, child.anchoredPosition + size / 2f);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static void Grow(ref bool any, ref Vector2 min, ref Vector2 max, Vector2 a, Vector2 b)
        {
            min = any ? Vector2.Min(min, a) : a;
            max = any ? Vector2.Max(max, b) : b;
            any = true;
        }

        static void StyleButton(Button button)
        {
            // The button's own image is the frame (it takes the hover tint); a faint dark fill sits behind the label.
            var image = button.GetComponent<Image>();
            image.sprite = frameSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.75f;
            image.color = Color.white;
            var fill = AddImage(button.transform, "Fill", fillSprite, new Color(0f, 0f, 0f, 0.35f), 1f);
            fill.transform.SetAsFirstSibling();
            Stretch(fill.rectTransform, -3f);
            var colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.8f, 0.75f, 0.7f);
            colors.highlightedColor = Amber;
            colors.selectedColor = colors.normalColor; // don't stay lit after a click
            colors.pressedColor = new Color(0.8f, 0.55f, 0.18f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.font = bold;
            label.color = Ink;
            label.fontStyle = FontStyles.UpperCase;
            label.characterSpacing = 4f;
        }

        static void StyleSlider(Slider slider)
        {
            var background = slider.transform.Find("Background")?.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = fillSprite;
                background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = 2f;
                background.color = new Color(0.2f, 0.21f, 0.22f, 0.9f);
            }
            if (slider.fillRect != null)
            {
                var fill = slider.fillRect.GetComponent<Image>();
                fill.sprite = fillSprite;
                fill.type = Image.Type.Sliced;
                fill.pixelsPerUnitMultiplier = 2f;
                fill.color = Amber;
            }
            if (slider.handleRect != null)
            {
                var handle = slider.handleRect.GetComponent<Image>();
                handle.sprite = fillSprite;
                handle.type = Image.Type.Sliced;
                handle.pixelsPerUnitMultiplier = 2f;
                handle.color = Ink;
                slider.handleRect.sizeDelta = new Vector2(14f, 0f);
            }
            var colors = slider.colors;
            colors.highlightedColor = Amber;
            colors.selectedColor = Color.white;
            slider.colors = colors;
        }

        // ------------------------------------------------------------------ preview

        /// <summary>
        /// Renders a UI prefab to a 1920x1080 PNG for checking the look without Play mode. Every child named in
        /// <paramref name="show"/> is switched on, siblings of those that are canvas-level panels are switched off.
        /// </summary>
        public static void RenderPreview(string prefabPath, string outPath, Color background, params string[] show)
        {
            var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            RenderCanvas(go, outPath, background, show);
            Object.DestroyImmediate(go);
        }

        /// <summary>Same as RenderPreview for the Main Menu scene's canvas (the scene is opened, not saved).</summary>
        public static void RenderMainMenuPreview(string outPath, params string[] show)
        {
            EditorSceneManager.OpenScene(MainMenuScene);
            RenderCanvas(GameObject.Find("MainMenu"), outPath, new Color(0.04f, 0.05f, 0.07f), show);
            EditorSceneManager.OpenScene(MainMenuScene); // discard the preview's changes
        }

        static void RenderCanvas(GameObject go, string outPath, Color background, string[] show)
        {
            var cam = new GameObject("UIPreviewCam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            int uiLayer = LayerMask.NameToLayer("UI");
            cam.cullingMask = 1 << uiLayer; // the UI only, not the open scene
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = uiLayer;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            if (show.Length > 0)
                foreach (Transform child in go.transform)
                    if (child.GetComponent<Image>() != null && child.Find("Title") != null) child.gameObject.SetActive(false);
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (System.Array.IndexOf(show, t.name) >= 0) t.gameObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(tex);
            rt.Release();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Two fading dividers pointing at a center point: the left one as drawn, the right one mirrored.</summary>
        static void Dividers(Transform parent, Vector2 anchor, float y, float gap, float width, float alpha)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var image = AddImage(parent, side < 0 ? "DividerLeft" : "DividerRight", dividerSprite,
                    new Color(Amber.r, Amber.g, Amber.b, alpha), 1f);
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = anchor;
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(width, 10f);
                rect.anchoredPosition = new Vector2(side * gap, y);
                rect.localScale = new Vector3(-side, 1f, 1f); // pivot on the cap, so the mirrored one grows rightward
            }
        }

        static Image AddImage(Transform parent, string name, Sprite sprite, Color color, float pixelsPerUnitMultiplier)
        {
            var go = new GameObject(Prefix + name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect, float outset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-outset, -outset);
            rect.offsetMax = new Vector2(outset, outset);
        }

        static void ClearOwn(Transform root)
        {
            var old = new List<GameObject>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(Prefix)) old.Add(t.gameObject);
            foreach (var go in old) Object.DestroyImmediate(go);
        }

        static GameObject Find(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        static bool IsCyan(Color c) => c.b > 0.8f && c.g > 0.7f && c.r < 0.6f;
    }
}
