using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        const string PistolModel = "Assets/Sci-fi Pistol/Models/Pistol.fbx";
        const string PistolTextures = "Assets/Sci-fi Pistol/Textures";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string ModelChild = "Model";

        // ------------------------------------------------------------------ pistol

        /// <summary>URP version of the Sci-fi Pistol's Built-in material (diffuse, metallic, normal, AO, cyan emission).</summary>
        static Material PistolMaterial()
        {
            string path = $"{MaterialFolder}/Pistol_URP.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{PistolTextures}/{n}.png");
            var normalImport = AssetImporter.GetAtPath($"{PistolTextures}/PistolNormal.png") as TextureImporter;
            if (normalImport != null && normalImport.textureType != TextureImporterType.NormalMap)
            {
                normalImport.textureType = TextureImporterType.NormalMap;
                normalImport.SaveAndReimport();
            }
            mat.SetTexture("_BaseMap", T("PistolDiffuse"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", T("PistolNormal"));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap", T("PistolMetallic"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness", 0.7f);
            mat.SetTexture("_OcclusionMap", T("PistolAO"));
            mat.SetTexture("_EmissionMap", T("PistolEmission"));
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", new Color(0.35f, 0.9f, 1f) * 2.5f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Instantiates the pistol model under parent with the URP material, no colliders, no shadows.</summary>
        static GameObject PistolInstance(Transform parent, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PistolModel);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.SetParent(parent, false);
            var mat = PistolMaterial();
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = mat;
                r.sharedMaterials = slots;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        /// <summary>
        /// Replaces the grey first-person gun box with the Sci-fi Pistol and dresses the Arc Pistol pickup. Also called by
        /// PlayerPrefabBuilder and Phase2ContentBuilder after they rebuild their prefabs.
        /// </summary>
        static readonly Vector3 GunViewPosition = new Vector3(0.2f, -0.27f, 0.42f);
        const float GunViewPitch = -6f;

        internal static string DressPistol()
        {
            EnsureFolder();
            string report = "";

            var player = PrefabUtility.LoadPrefabContents(PlayerPrefabBuilder.PlayerPath);
            try
            {
                var cam = player.transform.Find("CameraPivot");
                var oldGun = cam.Find("ArcPistol_Model");
                if (oldGun != null) Object.DestroyImmediate(oldGun.gameObject);

                var gun = PistolInstance(cam, "ArcPistol_Model");
                // Held low and tilted a little: the grip and trigger sit below the bottom edge of the screen, so only the slide and barrel show
                gun.transform.localPosition = GunViewPosition;
                gun.transform.localRotation = Quaternion.Euler(GunViewPitch, 0f, 0f);
                gun.transform.localScale = Vector3.one * 0.4f;
                gun.AddComponent<GunSway>(); // walk bob, recoil kick, look sway, reload tilt

                var muzzle = cam.Find("Muzzle");
                if (muzzle != null) muzzle.localPosition = new Vector3(0.2f, -0.13f, 0.62f);

                var pistol = player.GetComponent<ArcPistol>();
                var so = new SerializedObject(pistol);
                so.FindProperty("model").objectReferenceValue = gun;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabBuilder.PlayerPath);
                report = "player gun";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }

            report += ", pickup " + DressWithHolder($"{PrefabFolder}/Pickups/ArcPistolPickup.prefab", holder =>
            {
                var gun = PistolInstance(holder, "Pistol");
                var b = WorldBounds(gun);
                float s = 0.34f / b.size.z;                                   // about 34 cm long
                gun.transform.localScale = Vector3.one * s;
                b = WorldBounds(gun);
                gun.transform.position += holder.position - b.center;
            });
            return report;
        }

        // ------------------------------------------------------------------ pickups

        /// <summary>
        /// Hides a primitive pickup's mesh (the collider and scripts stay) and builds the real model under a "Model"
        /// holder that cancels the root's non-uniform scale, so parts can be sized in meters around the pickup center.
        /// </summary>
        static string DressWithHolder(string prefabPath, System.Action<Transform> build)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old = root.transform.Find(ModelChild);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var own = root.GetComponent<Renderer>();
                if (own != null) own.enabled = false;

                var holder = new GameObject(ModelChild).transform;
                holder.SetParent(root.transform, false);
                var s = root.transform.localScale;
                holder.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
                holder.localPosition = Vector3.zero;
                build(holder);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return "ok";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        const string BatteryModel = "Assets/_Project/Art/SketchFab/battery/source/Battery.fbx";
        const string BatteryTextures = "Assets/_Project/Art/SketchFab/battery/textures";
        const string FuseModel = "Assets/_Project/Art/SketchFab/cartridge fuse/source/FYUJ .fbx";

        static Material BatteryMaterial()
        {
            string path = $"{MaterialFolder}/Battery_URP.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{BatteryTextures}/Battery_Default_{n}.png");
            var normalImport = AssetImporter.GetAtPath($"{BatteryTextures}/Battery_Default_Normal.png") as TextureImporter;
            if (normalImport != null && normalImport.textureType != TextureImporterType.NormalMap)
            {
                normalImport.textureType = TextureImporterType.NormalMap;
                normalImport.SaveAndReimport();
            }
            mat.SetTexture("_BaseMap", T("Albedo"));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BumpMap", T("Normal"));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap", T("MetallicSmoothness"));
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Smoothness", 1f);
            mat.SetTexture("_OcclusionMap", T("AO"));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Instantiates a downloaded model under parent with colliders removed, uniformly scaled so its longest side is length, centered on parent.</summary>
        static GameObject FitModel(string assetPath, Transform parent, float length, Quaternion rotation, System.Func<string, Material> materialFor)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = rotation;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = materialFor(slots[i] != null ? slots[i].name.ToLowerInvariant() : "");
                r.sharedMaterials = slots;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            var b = WorldBounds(go);
            float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            go.transform.localScale *= length / longest;
            b = WorldBounds(go);
            go.transform.position += parent.position - b.center;
            return go;
        }

        /// <summary>Battery and override fuse, from the Sketchfab models (battery cell, cartridge fuse).</summary>
        internal static string DressPickups()
        {
            EnsureFolder();
            var battery = BatteryMaterial();
            var brass = Lit("Fuse_Brass", new Color(0.85f, 0.6f, 0.2f), 1f, 0.75f);
            var glass = Glass("Fuse_Glass", new Color(1f, 0.7f, 0.2f, 0.35f), Amber * 0.8f);
            var filament = Lit("Fuse_Filament", new Color(1f, 0.85f, 0.4f), 0f, 0.2f, new Color(4f, 2.8f, 0.6f));

            string b = DressWithHolder($"{PrefabFolder}/Pickups/BatteryClip.prefab", h =>
                FitModel(BatteryModel, h, 0.28f, Quaternion.identity, _ => battery));
            string f = DressWithHolder($"{PrefabFolder}/Pickups/OverrideFuse.prefab", h =>
                FitModel(FuseModel, h, 0.26f, Quaternion.Euler(0f, 0f, 90f), slot => slot.Contains("glass") ? glass : slot.Contains("matel") ? brass : filament));
            return $"battery={b}, fuse={f}";
        }
    }
}
