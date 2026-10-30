using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 4 art pass for props: puts the Poly Haven crate, barrel and medical box models on the SupplyCrate,
    /// ExplosiveBarrel and Medkit prefabs (the greybox shape's collider and the gameplay scripts stay; only its mesh is
    /// hidden), and scatters boulders and mossy rocks along the coast, the hill trail and the open ground. Rocks carve
    /// the NavMesh so enemies walk around them. Safe to re-run; Phase2ContentBuilder also calls DressPrefabs after it
    /// rebuilds the prefabs. Bridge command: art-props.
    /// </summary>
    public static class PropArtBuilder
    {
        const string ModelFolder = "Assets/_Project/Art/PolyHaven/Models";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string MaterialFolder = "Assets/_Project/Art/Props";
        const string IslandScene = "Assets/_Project/Scenes/IslandExterior.unity";
        const string ModelName = "Model";
        const string RockRootName = "ArtPass_Rocks";

        [MenuItem("Tools/Signal Lost/Art Pass – Props and Rocks")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            string props = DressPrefabs();

            var scene = EditorSceneManager.OpenScene(IslandScene);
            int rocks = PlaceRocks(Terrain.activeTerrain);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"art-props: {props}, rocks={rocks}");
        }

        // ------------------------------------------------------------------ prefabs

        /// <summary>Puts the models on the three prop prefabs. Returns a short report.</summary>
        public static string DressPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Props");

            var crateMat = ModelMaterial("Prop_WoodenCrate", "wooden_crate_01", "wooden_crate_01_diff_2k.jpg", "wooden_crate_01_nor_gl_2k.exr", 0.25f);
            var barrelMat = ModelMaterial("Prop_ExplosiveBarrel", "Barrel_01", "Barrel_01_explosive_diff_2k.png", "Barrel_01_explosive_nor_gl_2k.png", 0.45f, metallic: 0.6f);
            var medMat = ModelMaterial("Prop_MedicalBox", "medical_box", "medical_box_diff_2k.jpg", "medical_box_nor_gl_2k.exr", 0.4f);

            var crate = Dress($"{PrefabFolder}/Props/SupplyCrate.prefab", "wooden_crate_01/wooden_crate_01_2k.fbx", crateMat, "Body");
            var barrel = Dress($"{PrefabFolder}/Props/ExplosiveBarrel.prefab", "Barrel_01/Barrel_01_2k.fbx", barrelMat, "Body", "HazardStripe");
            var med = Dress($"{PrefabFolder}/Pickups/Medkit.prefab", "medical_box/medical_box_2k.fbx", medMat, null);
            return $"crate={crate}, barrel={barrel}, medkit={med}";
        }

        /// <summary>
        /// Hides the greybox renderers (bodyName child, or the root when null, plus any extra children), and fits the
        /// model inside the greybox shape's bounds: same footprint center, resting on the same bottom.
        /// </summary>
        static string Dress(string prefabPath, string modelPath, Material material, string bodyName, params string[] alsoHide)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{modelPath}");
            if (model == null) return "missing model";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var old = root.transform.Find(ModelName);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var body = bodyName == null ? root.transform : root.transform.Find(bodyName);
                var bodyRenderer = body.GetComponent<Renderer>();
                bodyRenderer.enabled = true; // bounds are only valid on an enabled renderer
                var target = bodyRenderer.bounds;
                bodyRenderer.enabled = false;
                foreach (var name in alsoHide)
                {
                    var extra = root.transform.Find(name);
                    if (extra != null) extra.GetComponent<Renderer>().enabled = false;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = ModelName;
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;
                foreach (var col in instance.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
                foreach (var r in instance.GetComponentsInChildren<Renderer>())
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    r.sharedMaterials = mats;
                }

                // Uniform world scale that fits inside the greybox shape; undo any (non-uniform) root scale
                var b = Bounds(instance);
                float s = Mathf.Min(target.size.x / b.size.x, target.size.y / b.size.y, target.size.z / b.size.z);
                var parentScale = root.transform.lossyScale;
                var local = instance.transform.localScale * s;
                instance.transform.localScale = new Vector3(local.x / parentScale.x, local.y / parentScale.y, local.z / parentScale.z);

                b = Bounds(instance);
                instance.transform.position += new Vector3(target.center.x - b.center.x, target.min.y - b.min.y, target.center.z - b.center.z);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return $"scale {s:F2}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>URP Lit material from a Poly Haven model's albedo and OpenGL normal map.</summary>
        static Material ModelMaterial(string name, string folder, string diffuse, string normal, float smoothness, float metallic = 0f)
        {
            string dir = $"{ModelFolder}/{folder}/textures";
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{diffuse}"));
            mat.SetColor("_BaseColor", Color.white);
            var normalPath = $"{dir}/{normal}";
            var importer = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ rocks

        struct RockMesh { public Mesh mesh; public Material material; public float size; }

        /// <summary>
        /// Boulders and mossy rocks: a ring along the shore, clusters beside the hill trail, and a few on open ground.
        /// Kept off roads and paths, away from buildings, patrol points and trees.
        /// </summary>
        static int PlaceRocks(Terrain terrain)
        {
            var old = GameObject.Find(RockRootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RockRootName).transform;

            var meshes = new List<RockMesh>();
            AddRockMeshes(meshes, "boulder_01/boulder_01_2k.fbx",
                ModelMaterial("Rock_Boulder", "boulder_01", "boulder_01_diff_2k.jpg", "boulder_01_nor_gl_2k.exr", 0.15f));
            AddRockMeshes(meshes, "rock_moss_set_01/rock_moss_set_01_2k.fbx",
                ModelMaterial("Rock_MossSet", "rock_moss_set_01", "rock_moss_set_01_diff_2k.jpg", "rock_moss_set_01_nor_gl_2k.exr", 0.1f));
            if (meshes.Count == 0) return 0;

            var data = terrain.terrainData;
            var blocked = IslandArtBuilder.BlockedAreas(terrain);
            var onPath = IslandArtBuilder.NearPath(data, 3f);
            var nearPath = IslandArtBuilder.NearPath(data, 9f);
            int res = data.alphamapResolution;

            var taken = new List<Vector2>();
            foreach (var tree in data.treeInstances) taken.Add(new Vector2(tree.position.x * data.size.x, tree.position.z * data.size.z));
            int treeCount = taken.Count;

            var rng = new System.Random(4021);
            int placed = 0;
            // (zone, how many, size range in meters)
            placed += Scatter(root, terrain, meshes, rng, blocked, onPath, nearPath, res, taken, treeCount, Zone.Shore, 40, 1.2f, 3.2f);
            placed += Scatter(root, terrain, meshes, rng, blocked, onPath, nearPath, res, taken, treeCount, Zone.Trail, 24, 0.6f, 1.8f);
            placed += Scatter(root, terrain, meshes, rng, blocked, onPath, nearPath, res, taken, treeCount, Zone.Open, 16, 0.5f, 1.5f);
            return placed;
        }

        enum Zone { Shore, Trail, Open }

        static int Scatter(Transform root, Terrain terrain, List<RockMesh> meshes, System.Random rng, List<Rect> blocked,
            bool[,] onPath, bool[,] nearPath, int res, List<Vector2> taken, int treeCount, Zone zone, int count, float minSize, float maxSize)
        {
            var data = terrain.terrainData;
            int placed = 0;
            for (int attempt = 0; attempt < count * 60 && placed < count; attempt++)
            {
                float x = 24f + (float)rng.NextDouble() * 202f;
                float z = 8f + (float)rng.NextDouble() * 223f;
                float u = x / data.size.x, v = z / data.size.z;
                float h = data.GetInterpolatedHeight(u, v);
                int ax = Mathf.RoundToInt(u * (res - 1)), az = Mathf.RoundToInt(v * (res - 1));

                bool ok = zone switch
                {
                    Zone.Shore => h > 3.6f && h < 5.9f,
                    Zone.Trail => h > 8f && nearPath[az, ax] && !onPath[az, ax],
                    _ => h >= 5.9f && h < 8f && !nearPath[az, ax]
                };
                if (!ok || onPath[az, ax] || data.GetSteepness(u, v) > 35f) continue;
                var p = new Vector2(x, z);
                if (IslandArtBuilder.IsBlocked(blocked, p)) continue;

                float size = Mathf.Lerp(minSize, maxSize, (float)rng.NextDouble());
                bool crowded = false;
                for (int i = 0; i < taken.Count; i++)
                {
                    float gap = i < treeCount ? 1.5f + size * 0.5f : size + 1.5f;
                    if ((taken[i] - p).sqrMagnitude < gap * gap) { crowded = true; break; }
                }
                if (crowded) continue;

                var rock = meshes[rng.Next(meshes.Count)];
                var go = new GameObject($"Rock_{zone}_{placed}");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = rock.mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = rock.material;
                go.AddComponent<MeshCollider>().sharedMesh = rock.mesh;
                go.layer = LayerMask.NameToLayer("Ground");

                float scale = size / rock.size;
                go.transform.localScale = Vector3.one * scale * Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble());
                // Lay the rock on its flattest side (the mesh's shortest axis points up), then a random yaw and slight tilt
                var ms = rock.mesh.bounds.size;
                var flat = ms.x <= ms.y && ms.x <= ms.z ? Quaternion.Euler(0, 0, 90) : ms.z <= ms.y ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;
                go.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 12f - 6f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 12f - 6f) * flat;
                // Sink a quarter of the rock into the ground so it doesn't float on slopes (bounds measured at the origin)
                var bounds = go.GetComponent<MeshRenderer>().bounds;
                go.transform.position = new Vector3(x, h - bounds.min.y - bounds.size.y * 0.25f, z);

                var local = rock.mesh.bounds;
                var obstacle = go.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.center = local.center;
                obstacle.size = local.size;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

                taken.Add(p);
                placed++;
            }
            return placed;
        }

        /// <summary>Every mesh in the FBX, with its largest dimension in meters (model import scale applied).</summary>
        static void AddRockMeshes(List<RockMesh> list, string modelPath, Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{modelPath}");
            if (model == null) { Debug.LogWarning($"[Signal Lost] Missing rock model {modelPath}"); return; }
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                var s = mesh.bounds.size;
                float size = Mathf.Max(s.x, Mathf.Max(s.y, s.z));
                if (size > 0.01f) list.Add(new RockMesh { mesh = mesh, material = material, size = size });
            }
        }
    }
}
