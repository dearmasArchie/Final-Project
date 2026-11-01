using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Main menu background: renders a few views of the island scene into pictures, then puts a MenuBackground slideshow
    /// behind the menu panels. Bridge command: menu-bg. Phase3ContentBuilder re-applies it when it rebuilds the menu scene.
    /// </summary>
    public static class MenuBackgroundBuilder
    {
        const string Folder = "Assets/_Project/Art/MenuBackground";
        const string IslandScene = "Assets/_Project/Scenes/IslandExterior.unity";
        const string MenuScene = "Assets/_Project/Scenes/MainMenu.unity";

        // Camera spot and target as (x, height above ground, z); heights are resolved against the terrain
        static readonly (string name, Vector3 from, Vector3 look)[] Views =
        {
            ("tower", new Vector3(80f, 34f, 120f), new Vector3(130f, 40f, 194f)),
            ("pier", new Vector3(100f, 12f, 40f), new Vector3(140f, 6f, 22f)),
            ("camp", new Vector3(70f, 14f, 25f), new Vector3(105f, 8f, 75f)),
            ("coast", new Vector3(-30f, 8f, 90f), new Vector3(110f, 22f, 120f)),
        };

        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "MenuBackground");

            EditorSceneManager.OpenScene(IslandScene, OpenSceneMode.Single);
            var terrain = Terrain.activeTerrain;
            var go = new GameObject("MenuBackgroundCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.farClipPlane = 600f;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            foreach (var v in Views)
            {
                float ground = terrain != null ? terrain.SampleHeight(new Vector3(v.from.x, 0, v.from.z)) + terrain.transform.position.y : 0f;
                go.transform.position = new Vector3(v.from.x, Mathf.Max(ground, 6f) + v.from.y, v.from.z);
                go.transform.LookAt(v.look);
                var rt = new RenderTexture(1920, 1080, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"{Folder}/bg_{v.name}.png", tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            foreach (var v in Views)
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath($"{Folder}/bg_{v.name}.png");
                imp.textureType = TextureImporterType.Default;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 2048;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.SaveAndReimport();
            }

            EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            Apply();
            EditorSceneManager.SaveOpenScenes();
            EditorCommandBridge.Log($"menu-bg: {Views.Length} island pictures rendered, slideshow added to the main menu");
        }

        /// <summary>Adds (or replaces) the slideshow behind the panels of the open main menu scene. No-op when the pictures don't exist yet.</summary>
        public static void Apply()
        {
            var canvas = Object.FindFirstObjectByType<MainMenuController>();
            if (canvas == null) { EditorCommandBridge.Log("menu-bg: no MainMenuController in the open scene"); return; }
            var pictures = new System.Collections.Generic.List<Texture2D>();
            foreach (var v in Views)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/bg_{v.name}.png");
                if (t != null) pictures.Add(t);
            }
            if (pictures.Count == 0) { EditorCommandBridge.Log("menu-bg: pictures not imported yet"); return; }

            var old = canvas.transform.Find("Background");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = new GameObject("Background", typeof(RectTransform), typeof(RectMask2D));
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsFirstSibling();
            Stretch(root.GetComponent<RectTransform>());

            RawImage Layer(string name)
            {
                var g = new GameObject(name, typeof(RectTransform), typeof(RawImage));
                g.transform.SetParent(root.transform, false);
                Stretch(g.GetComponent<RectTransform>());
                var img = g.GetComponent<RawImage>();
                img.raycastTarget = false;
                return img;
            }
            var a = Layer("PictureA");
            var b = Layer("PictureB");
            a.texture = pictures[0]; // visible in the editor too; the slideshow takes over when the menu runs
            b.color = new Color(1f, 1f, 1f, 0f);

            // Darkening veil so the menu text stays readable on a bright picture
            var veilGo = new GameObject("Veil", typeof(RectTransform), typeof(Image));
            veilGo.transform.SetParent(root.transform, false);
            Stretch(veilGo.GetComponent<RectTransform>());
            var veil = veilGo.GetComponent<Image>();
            veil.color = new Color(0.02f, 0.03f, 0.05f, 0.45f);
            veil.raycastTarget = false;

            var show = root.AddComponent<MenuBackground>();
            var so = new SerializedObject(show);
            var list = so.FindProperty("pictures");
            list.arraySize = pictures.Count;
            for (int i = 0; i < pictures.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pictures[i];
            so.FindProperty("front").objectReferenceValue = a;
            so.FindProperty("back").objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.SolidColor;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
