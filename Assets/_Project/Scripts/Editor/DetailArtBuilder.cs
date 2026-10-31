using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Detail art pass: swaps the last generic boxes for real models. The Sci-fi Pistol on the player and as the
    /// pickup, a battery cell and an override fuse built from parts, Poly Haven power cabinets on the main power
    /// switch and the hub, a steel cage freight elevator in both scenes, a lattice radio tower with dish and beacon,
    /// and industrial dressing for the underground rooms (beams, pillars, pipe runs, door frames, caged lamps,
    /// shelves). Everything lives under an "ArtPass_Details" object per scene (rebuilt each run); greybox shapes keep
    /// their colliders and scripts and only lose their renderer. Safe to re-run. Bridge command: art-details.
    /// </summary>
    public static partial class DetailArtBuilder
    {
        const string ModelFolder = "Assets/_Project/Art/PolyHaven/Models";
        const string TextureFolder = "Assets/_Project/Art/PolyHaven/Textures";
        const string MaterialFolder = "Assets/_Project/Art/Details";
        const string IslandScene = "Assets/_Project/Scenes/IslandExterior.unity";
        const string BunkerScene = "Assets/_Project/Scenes/UndergroundFacility.unity";
        const string RootName = "ArtPass_Details";
        const string TriplanarShader = "SignalLost/Triplanar Lit";

        static readonly Color Cyan = new Color(0.2f, 0.85f, 1f);
        static readonly Color Amber = new Color(1f, 0.62f, 0.12f);

        [MenuItem("Tools/Signal Lost/Art Pass – Details")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            EnsureFolder();
            string pistol = DressPistol();
            string pickups = DressPickups();

            var island = EditorSceneManager.OpenScene(IslandScene);
            string islandReport = DressIsland();
            EditorSceneManager.MarkSceneDirty(island);
            EditorSceneManager.SaveScene(island);

            var bunker = EditorSceneManager.OpenScene(BunkerScene);
            string bunkerReport = DressBunker();
            EditorSceneManager.MarkSceneDirty(bunker);
            EditorSceneManager.SaveScene(bunker);

            EditorSceneManager.OpenScene(IslandScene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"art-details: pistol={pistol}; pickups={pickups}; island: {islandReport}; bunker: {bunkerReport}");
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Details");
        }

        // ------------------------------------------------------------------ materials

        /// <summary>Plain URP Lit asset, created once and updated on later runs.</summary>
        static Material Lit(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                mat.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>See-through tinted glass with a glow.</summary>
        static Material Glass(string name, Color color, Color emission)
        {
            var mat = Lit(name, color, 0f, 0.9f, emission);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>World-space metal_plate or concrete material (same shader as the bunker walls, so cubes of any size never smear).</summary>
        static Material Triplanar(string name, string texture, Color tint, float tile, float metallic, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(TriplanarShader));
                AssetDatabase.CreateAsset(mat, path);
            }
            string dir = $"{TextureFolder}/{texture}";
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{texture}_diff_2k.jpg"));
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{texture}_nor_gl_2k.jpg"));
            mat.SetTexture("_ARMMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{texture}_arm_2k.jpg"));
            mat.SetColor("_BaseColor", tint);
            mat.SetColor("_GreyboxColor", Color.white);
            mat.SetFloat("_TileSize", tile);
            mat.SetFloat("_MetallicScale", metallic);
            mat.SetFloat("_SmoothnessScale", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Steel(string name, Color tint) => Triplanar(name, "metal_plate", tint, 1.5f, 0.55f, 0.55f);

        static Texture2D Tex(string dir, string stem)
        {
            foreach (var ext in new[] { "jpg", "png", "exr" })
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{stem}.{ext}");
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>URP Lit material from a Poly Haven model's textures; stem is the file prefix, e.g. "power_box_01".</summary>
        static Material ModelMat(string folder, string stem, float smoothness, float metallic, bool emissive = false)
        {
            string dir = $"{ModelFolder}/{folder}/textures";
            string path = $"{MaterialFolder}/{stem}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", Tex(dir, $"{stem}_diff_2k"));
            mat.SetColor("_BaseColor", Color.white);
            var normalImport = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Tex(dir, $"{stem}_nor_gl_2k"))) as TextureImporter;
            if (normalImport != null && normalImport.textureType != TextureImporterType.NormalMap)
            {
                normalImport.textureType = TextureImporterType.NormalMap;
                normalImport.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap", Tex(dir, $"{stem}_nor_gl_2k"));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            var glow = emissive ? Tex(dir, $"{stem}_emissive_2k") : null;
            if (glow != null)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                mat.SetTexture("_EmissionMap", glow);
                mat.SetColor("_EmissionColor", Color.white * 2.5f);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ placing models and parts

        enum Anchor { Bottom, Center, Top }

        /// <summary>
        /// Instantiates a Poly Haven model (no colliders) under parent, uniformly scaled, so its bounds are anchored at
        /// pos: bounds center in x and z, and the bottom, middle or top in y. Materials: the main one, except slots
        /// whose imported name contains one of the extra keys.
        /// </summary>
        static GameObject Model(string folder, string file, Transform parent, Vector3 pos, float yaw, float scale, Anchor anchor,
            Material main, params (string key, Material mat)[] extra)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{folder}/{file}_2k.fbx");
            if (asset == null) { Debug.LogWarning($"[Signal Lost] Missing model {folder}/{file}"); return null; }
            var holder = new GameObject(folder);
            holder.transform.SetParent(parent, false);
            holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.transform.SetParent(holder.transform, false);
            holder.transform.localScale = Vector3.one * scale;
            foreach (var c in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    string slotName = slots[i] != null ? slots[i].name.ToLowerInvariant() : "";
                    var chosen = main ?? slots[i];                    // null main keeps the model's own materials
                    foreach (var (key, mat) in extra) if (slotName.Contains(key)) chosen = mat;
                    slots[i] = chosen;
                }
                r.sharedMaterials = slots;
            }
            var b = WorldBounds(holder);
            float y = anchor == Anchor.Bottom ? b.min.y : anchor == Anchor.Top ? b.max.y : b.center.y;
            holder.transform.position += new Vector3(pos.x - b.center.x, pos.y - y, pos.z - b.center.z);
            return holder;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Primitive part (collider removed unless asked for), positioned relative to parent.</summary>
        static GameObject Part(Transform parent, string name, PrimitiveType shape, Vector3 localPos, Vector3 localScale, Material mat,
            Quaternion? rotation = null, bool collider = false, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = localScale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            if (!shadows) r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>Thin square bar between two world points (lattice members, rails, cables).</summary>
        static GameObject Bar(Transform parent, string name, Vector3 a, Vector3 b, float thickness, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            Vector3 d = b - a;
            go.transform.position = (a + b) * 0.5f;
            go.transform.rotation = d.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(d.normalized, Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up) : Quaternion.identity;
            go.transform.localScale = new Vector3(thickness, thickness, d.magnitude);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static Transform FreshRoot()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            return new GameObject(RootName).transform;
        }

        static void HideRenderer(GameObject go)
        {
            if (go == null) return;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        static void Static(GameObject go) => GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

        static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = pos;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        static GameObject Find(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) Debug.LogWarning($"[Signal Lost] art-details: '{name}' not found in {EditorSceneManager.GetActiveScene().name}");
            return go;
        }

        /// <summary>Rendered preview of a model on a plain background (front, side, top), for checking orientation.</summary>
        public static void RenderPreview(string folder, string file, string outPng, float scale = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{folder}/{file}_2k.fbx");
            var preview = new PreviewRenderUtility();
            try
            {
                var instance = Object.Instantiate(asset);
                foreach (var r in instance.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{file}.mat") ?? mats[i];
                    r.sharedMaterials = mats;
                }
                preview.AddSingleGO(instance);
                var b = WorldBounds(instance);
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.25f, 0.27f, 0.3f);
                preview.camera.nearClipPlane = 0.01f;
                preview.camera.farClipPlane = 100f;
                preview.lights[0].intensity = 1.4f;
                float dist = b.size.magnitude * 1.4f;
                var views = new[] { new Vector3(15, 25, 0), new Vector3(15, 115, 0), new Vector3(15, 205, 0) };
                var sheet = new Texture2D(views.Length * 400, 400, TextureFormat.RGBA32, false);
                for (int i = 0; i < views.Length; i++)
                {
                    preview.camera.transform.rotation = Quaternion.Euler(views[i]);
                    preview.camera.transform.position = b.center - preview.camera.transform.forward * dist;
                    preview.BeginPreview(new Rect(0, 0, 400, 400), GUIStyle.none);
                    preview.camera.Render();
                    var tex = preview.EndPreview() as RenderTexture;
                    var old = RenderTexture.active;
                    RenderTexture.active = tex;
                    sheet.ReadPixels(new Rect(0, 0, 400, 400), i * 400, 0);
                    RenderTexture.active = old;
                }
                sheet.Apply();
                System.IO.File.WriteAllBytes(outPng, sheet.EncodeToPNG());
                Object.DestroyImmediate(sheet);
            }
            finally
            {
                preview.Cleanup();
            }
        }
    }
}
