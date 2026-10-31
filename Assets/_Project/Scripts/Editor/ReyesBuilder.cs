using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Dr. Reyes as a real character: builds a prefab from the "scientist" FBX (Art/Dr. Reyes) with one coherent outfit (head,
    /// hair, eyes, glasses, shirt, lab coat, pants, body), URP materials from its textures, the Mixamo Sitting Idle animation,
    /// and a splinted leg. Dress() then swaps the greybox capsule's look for it and sits her on a bedroll, keeping the capsule's
    /// collider and dialogue. Run by the island detail pass (art-details).
    /// </summary>
    public static class ReyesBuilder
    {
        const string Folder = "Assets/_Project/Art/Dr. Reyes";
        const string FbxPath = Folder + "/source/scientist.fbx";
        const string TexFolder = Folder + "/textures";
        const string MatFolder = Folder + "/Materials";
        const string PrefabPath = "Assets/_Project/Prefabs/NPC/Dr_Reyes_Model.prefab";
        const string ControllerPath = Folder + "/Reyes.controller";
        const string SitClipFbx = "Assets/_Project/Art/Mixamo/Sitting Idle.fbx";

        // The kit holds variants of several characters; these source-material names are the outfit that is kept
        static readonly (string key, string albedo, string normal, bool cutout, Color tint, float smooth)[] Outfit =
        {
            ("officerhans_head", "officerhans_head_col", "officerhans_head_local", false, Color.white, 0.25f),
            ("officerhans_hair", "officerhans_hair", null, false, new Color(1.3f, 1.1f, 1.0f), 0.3f),
            ("common_eye", "common_eye", null, true, Color.white, 0.8f),
            ("engineer_glasses", "engineer_glasses", null, false, Color.white, 0.7f),
            ("scientist_shirt", "scientist_shirt_row", "scientist_shirt_row_local", false, Color.white, 0.15f),
            ("scientist_jacket01", "scientist_jacket01_row", "scientist_jacket01_row_local", false, Color.white, 0.2f),
            ("scientist_pants01", "scientist_pants01", "scientist_pants01_local", false, Color.white, 0.15f),
            ("offduty_body", null, null, false, new Color(0.66f, 0.46f, 0.36f), 0.3f), // no skin texture in the kit: a skin tone
        };

        public static string BuildPrefab()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbx == null) return "scientist.fbx missing";
            Directory.CreateDirectory(MatFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrepareTextures();
            EnsureSitLoop();

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "Dr_Reyes_Model";
            int kept = 0, removed = 0;
            foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToArray())
            {
                string source = smr.sharedMaterials.Length > 0 && smr.sharedMaterials[0] != null ? smr.sharedMaterials[0].name : "";
                var match = Outfit.FirstOrDefault(o => source.Contains(o.key));
                if (match.key == null) { Object.DestroyImmediate(smr.gameObject); removed++; continue; }
                var mat = Material(match.key, match.albedo, match.normal, match.cutout, match.tint, match.smooth);
                var mats = new Material[smr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                smr.sharedMaterials = mats;
                smr.updateWhenOffscreen = true; // the seated pose moves bones far from the bind-pose bounds
                kept++;
            }

            AddSplint(instance.transform);

            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Avatar>().FirstOrDefault();
            animator.runtimeAnimatorController = BuildController();
            animator.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            return $"{kept} parts kept, {removed} variants removed";
        }

        static AnimatorController BuildController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var clip = AssetDatabase.LoadAllAssetsAtPath(SitClipFbx).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
            var state = controller.layers[0].stateMachine.AddState("SittingIdle");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            return controller;
        }

        /// <summary>The Sitting Idle clip must loop; EnemyArtBuilder does this too, but the NPC must not depend on it having run.</summary>
        static void EnsureSitLoop()
        {
            var importer = AssetImporter.GetAtPath(SitClipFbx) as ModelImporter;
            if (importer == null) return;
            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips.All(c => c.loopTime)) return;
            foreach (var c in clips) c.loopTime = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        static void PrepareTextures()
        {
            foreach (var o in Outfit)
                foreach (var name in new[] { o.albedo, o.normal })
                {
                    if (name == null) continue;
                    var importer = AssetImporter.GetAtPath($"{TexFolder}/{name}.png") as TextureImporter;
                    if (importer == null) continue;
                    bool normal = name.EndsWith("_local");
                    if (importer.maxTextureSize <= 1024 && (importer.textureType == TextureImporterType.NormalMap) == normal) continue;
                    importer.maxTextureSize = 1024;
                    importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    importer.SaveAndReimport();
                }
        }

        static Material Material(string key, string albedo, string normal, bool cutout, Color tint, float smoothness)
        {
            string path = $"{MatFolder}/Reyes_{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", albedo != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/{albedo}.png") : null);
            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/{normal}.png"));
                mat.EnableKeyword("_NORMALMAP");
            }
            if (cutout)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A bandaged, splinted right shin (she has a broken leg): cloth wraps and two boards parented to the leg bone.</summary>
        static void AddSplint(Transform root)
        {
            Transform Find(string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            var leg = Find("rightleg");
            var foot = Find("rightfoot");
            if (leg == null || foot == null) return;
            var cloth = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Reyes_Bandage" };
            cloth.SetColor("_BaseColor", new Color(0.86f, 0.82f, 0.72f));
            cloth.SetFloat("_Smoothness", 0.1f);
            var wood = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Reyes_Splint" };
            wood.SetColor("_BaseColor", new Color(0.45f, 0.32f, 0.2f));
            wood.SetFloat("_Smoothness", 0.2f);
            foreach (var m in new[] { cloth, wood })
            {
                string path = $"{MatFolder}/{m.name}.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(m, path);
            }

            Vector3 a = leg.position, b = foot.position;
            Vector3 dir = (b - a).normalized;
            float length = (b - a).magnitude;
            var group = new GameObject("Splint").transform;
            group.SetParent(leg, true);
            for (int i = 0; i < 5; i++)
            {
                var wrap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wrap.name = "Wrap";
                Object.DestroyImmediate(wrap.GetComponent<Collider>());
                wrap.transform.SetParent(group, true);
                wrap.transform.position = a + dir * (length * (0.2f + i * 0.14f));
                wrap.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                wrap.transform.localScale = new Vector3(0.17f, 0.035f, 0.17f);
                wrap.GetComponent<Renderer>().sharedMaterial = cloth;
            }
            foreach (float side in new[] { -1f, 1f })
            {
                var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
                board.name = "SplintBoard";
                Object.DestroyImmediate(board.GetComponent<Collider>());
                board.transform.SetParent(group, true);
                Vector3 across = Vector3.Cross(dir, Vector3.forward).normalized;
                board.transform.position = a + dir * (length * 0.5f) + across * side * 0.075f;
                board.transform.rotation = Quaternion.LookRotation(Vector3.Cross(across, dir), dir);
                board.transform.localScale = new Vector3(0.03f, length * 0.62f, 0.05f);
                board.GetComponent<Renderer>().sharedMaterial = wood;
            }
        }

        // ------------------------------------------------------------------ scene

        public static string Dress(Transform root)
        {
            var reyes = GameObject.Find("Dr_Reyes");
            if (reyes == null) return "no Dr_Reyes";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                string built = BuildPrefab();
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return built;
            }

            var capsule = reyes.GetComponent<Renderer>();
            if (capsule != null) capsule.enabled = false;           // the collider and DialogueSource stay
            var old = reyes.transform.Find("ReyesVisual");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            // A holder that cancels the capsule's scale, standing on the ground, facing the way the capsule faces
            var terrain = Terrain.activeTerrain;
            float ground = terrain != null ? terrain.SampleHeight(reyes.transform.position) + terrain.transform.position.y : reyes.transform.position.y - 0.7f;
            var holder = new GameObject("ReyesVisual").transform;
            holder.SetParent(reyes.transform, false);
            Vector3 s = reyes.transform.lossyScale;
            holder.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            holder.position = new Vector3(reyes.transform.position.x, ground, reyes.transform.position.z);
            holder.rotation = reyes.transform.rotation;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), holder);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Sitting Idle is a seated-on-the-floor pose, so she gets a bedroll under her and a medical box beside her
            var props = new GameObject("ReyesCamp").transform;
            props.SetParent(root, false);
            Quaternion rot = holder.rotation;
            var mat = new Color(0.2f, 0.26f, 0.18f);
            var matMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Details/Stool_Seat.mat");
            var blanket = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Details/Pier_Net.mat");
            Place(props, "Bedroll", PrimitiveType.Cube, holder.position + rot * new Vector3(0f, 0.015f, -0.1f), new Vector3(1.3f, 0.03f, 1.8f), rot, matMaterial);
            Place(props, "Blanket", PrimitiveType.Cube, holder.position + rot * new Vector3(-0.55f, 0.04f, -0.55f), new Vector3(0.5f, 0.05f, 0.6f), rot * Quaternion.Euler(0f, 15f, 0f), blanket);
            _ = mat;
            model.transform.localPosition = new Vector3(0f, 0.03f / holder.lossyScale.y, 0f); // on top of the bedroll
            return $"model placed at ({holder.position.x:F1},{holder.position.y:F1},{holder.position.z:F1}), bedroll";
        }

        static void Place(Transform parent, string name, PrimitiveType shape, Vector3 worldPos, Vector3 size, Quaternion rot, Material mat)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(worldPos, rot);
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
