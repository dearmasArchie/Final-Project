using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Creates the post-processing profiles (ACES tonemapping, bloom, color grading, vignette) and puts a global
    /// Volume in each scene. The bunker uses a darker, cooler variant. Bridge command: postfx.
    /// </summary>
    public static class PostProcessBuilder
    {
        const string Folder = "Assets/_Project/Settings";
        const string VolumeName = "PostProcessVolume";

        [MenuItem("Tools/Signal Lost/Build Post-Processing")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project", "Settings");

            var outdoor = Profile("PostFX_Outdoor", bloom: 0.9f, contrast: 12f, saturation: -4f, vignette: 0.25f, filter: Color.white);
            var bunker = Profile("PostFX_Bunker", bloom: 1.1f, contrast: 18f, saturation: -12f, vignette: 0.38f, filter: new Color(0.9f, 0.95f, 1f));

            PlaceVolume("Assets/_Project/Scenes/IslandExterior.unity", outdoor);
            PlaceVolume("Assets/_Project/Scenes/UndergroundFacility.unity", bunker);
            PlaceVolume("Assets/_Project/Scenes/MainMenu.unity", outdoor);
            PlaceVolume("Assets/_Project/Scenes/Sandbox_A.unity", outdoor);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/IslandExterior.unity");
            Debug.Log("[Signal Lost] Post-processing built.");
        }

        static VolumeProfile Profile(string name, float bloom, float contrast, float saturation, float vignette, Color filter)
        {
            var path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var b = profile.Add<Bloom>(true);
            b.intensity.Override(bloom);
            b.threshold.Override(1f);
            b.scatter.Override(0.65f);

            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.colorFilter.Override(filter);

            var v = profile.Add<Vignette>(true);
            v.intensity.Override(vignette);
            v.smoothness.Override(0.4f);

            // Volume components must live inside the profile asset or they are lost on reload
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void PlaceVolume(string scenePath, VolumeProfile profile)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) return;
            var scene = EditorSceneManager.OpenScene(scenePath);
            var old = GameObject.Find(VolumeName);
            if (old != null) Object.DestroyImmediate(old);

            var volume = new GameObject(VolumeName).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;

            // Cameras must have post-processing switched on to show it
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                EditorUtility.SetDirty(data);
            }
            EditorSceneManager.SaveScene(scene);
        }
    }
}
