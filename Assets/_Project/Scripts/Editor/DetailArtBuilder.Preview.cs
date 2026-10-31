using UnityEditor;
using UnityEngine;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        /// <summary>Renders the open scene from a temporary camera to a PNG (no post-processing).</summary>
        public static void RenderView(Vector3 position, Vector3 lookAt, string outPng, float fov = 65f, int width = 1280, int height = 720)
        {
            var go = new GameObject("TempViewCamera");
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = fov;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 600f;
                cam.clearFlags = CameraClearFlags.Skybox;
                go.transform.position = position;
                go.transform.LookAt(lookAt);
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                var old = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = old;
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPng));
                System.IO.File.WriteAllBytes(outPng, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Creates the model materials and renders a front/side/back contact sheet per model into outDir.</summary>
        public static void PreviewAll(string outDir)
        {
            EnsureFolder();
            System.IO.Directory.CreateDirectory(outDir);
            ModelMat("power_box_01", "power_box_01", 0.4f, 0.5f);
            ModelMat("utility_box_01", "utility_box_01", 0.4f, 0.5f);
            ModelMat("caged_hanging_light", "caged_hanging_light", 0.4f, 0.6f, emissive: true);
            ModelMat("industrial_caged_sconce", "industrial_caged_sconce", 0.4f, 0.6f, emissive: true);
            ModelMat("steel_frame_shelves_01", "steel_frame_shelves_01", 0.4f, 0.6f);
            ModelMat("portable_generator", "portable_generator", 0.4f, 0.3f);
            ModelMat("vintage_radio_transceiver", "vintage_radio_transceiver", 0.4f, 0.4f);
            ModelMat("rollershutter_door", "rollershutter_door", 0.4f, 0.6f);
            foreach (var m in new[] { "power_box_01", "utility_box_01", "caged_hanging_light", "industrial_caged_sconce", "steel_frame_shelves_01", "portable_generator", "vintage_radio_transceiver", "rollershutter_door" })
                RenderPreview(m, m, $"{outDir}/{m}.png");
        }
    }
}
