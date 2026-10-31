using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 4 art pass for enemies and the boat: puts the Sci-Fi Robots Bundle robots (Robert = Enforcer,
    /// Engie = Aegis Warden, animated with the Mixamo clips), the Buster Drone (Recon Stinger), the Yellow Heavy Robot
    /// mech (Unit 00-Titan, animated in code by MechWalkBob) and the motorboat on the greybox prefabs and scene object.
    /// The greybox colliders, weak points, shields and scripts stay; only the greybox meshes are hidden, and the
    /// visor weak points and muzzles follow the robots' head and hand bones. Also shrinks the Titan's 8K textures to
    /// 2K once. Safe to re-run; the Phase 2/3 builders call DressEnemies after rebuilding the prefabs.
    /// Bridge command: art-enemies.
    /// </summary>
    public static class EnemyArtBuilder
    {
        /// <summary>The Enforcer is 1.5x a normal robot (about 2.8 m tall).</summary>
        const float EnforcerScale = 1.5f;

        const string EnemyFolder = "Assets/_Project/Prefabs/Enemies";
        const string OutFolder = "Assets/_Project/Art/Enemies";
        const string MixamoFolder = "Assets/_Project/Art/Mixamo";
        const string RobotFolder = "Assets/Same Gev Dudios/Sci-Fi Robots Bundle";
        const string DroneFolder = "Assets/_Project/Art/SketchFab/BusterDrone";
        const string TitanFolder = "Assets/_Project/Art/SketchFab/TitanRobot";
        const string BoatFolder = "Assets/_Project/Art/SketchFab/Motorboat";
        const string IslandScene = "Assets/_Project/Scenes/IslandExterior.unity";
        const string ModelName = "Model";

        [MenuItem("Tools/Signal Lost/Art Pass – Enemies and Boat")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            string textures = ShrinkTitanTextures();
            string report = DressEnemies();

            var scene = EditorSceneManager.OpenScene(IslandScene);
            string boat = DressBoat();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"art-enemies: titan textures {textures}; {report}; boat {boat}");
        }

        /// <summary>Imports the animations, builds the controllers and dresses all four enemy prefabs.</summary>
        public static string DressEnemies()
        {
            if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Enemies");
            var clips = ImportMixamo();
            var robert = MakeHumanoid($"{RobotFolder}/Models/Robert.fbx");
            var engie = MakeHumanoid($"{RobotFolder}/Models/Engie.fbx");

            var rifle = RobotController("Enforcer", clips["Rifle Idle"], clips["Rifle Walk"], clips["Run Forward"],
                clips["Firing Rifle"], clips["Hit Reaction"], clips["Mutant Dying"]);
            var shield = RobotController("AegisWarden", clips["Sword And Shield Idle"], clips["Sword And Shield Walk"], clips["Sword And Shield Walk"],
                clips["Sword And Shield Idle"], clips["Hit Reaction"], clips["Mutant Dying"]);

            var parts = new List<string>
            {
                "enforcer " + DressRobot("Enforcer", $"{RobotFolder}/Prefabs/Robert.prefab", robert, rifle, 1.85f,
                    visor: "Visor_WeakPoint", hide: new[] { "Body", "Head", "ArmCannon" }, muzzleBone: HumanBodyBones.RightHand, cannon: null,
                    scale: EnforcerScale),
                "aegis " + DressRobot("AegisWarden", $"{RobotFolder}/Prefabs/Engie.prefab", engie, shield, 2.35f,
                    visor: "Visor_WeakPoint", hide: new[] { "Body", "Head" }, muzzleBone: HumanBodyBones.UpperChest, cannon: "ShoulderCannon"),
                "stinger " + DressStinger(),
                "titan " + DressTitan()
            };
            parts.Add("fx " + AddDeathEffects());
            parts.Add("shields " + ShieldBubble("AegisWarden", "EnergyShield", 2.7f, 0.9f) + "/" + ShieldBubble("Unit00_Titan", "RiotShield", 6.4f, 3.1f));
            return string.Join(", ", parts);
        }

        // ------------------------------------------------------------------ death explosions and shields

        const string ExplosionFolder = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs";

        /// <summary>SmallExplosion on the robots and drone, BigExplosion on the Titan, centered on each body.</summary>
        static string AddDeathEffects()
        {
            var small = AssetDatabase.LoadAssetAtPath<GameObject>($"{ExplosionFolder}/SmallExplosion.prefab");
            var big = AssetDatabase.LoadAssetAtPath<GameObject>($"{ExplosionFolder}/BigExplosion.prefab");
            if (small == null || big == null) return "missing Particle Pack prefabs";
            Explosion("Enforcer", small, EnforcerScale, new Vector3(0, 1.1f, 0));
            Explosion("AegisWarden", small, 1.4f, new Vector3(0, 1.3f, 0));
            Explosion("ReconStinger", small, 0.7f, Vector3.zero);
            Explosion("Unit00_Titan", big, 1.6f, new Vector3(0, 3f, 0));
            return "ok";
        }

        static void Explosion(string prefabName, GameObject effect, float scale, Vector3 offset)
        {
            string path = $"{EnemyFolder}/{prefabName}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var fx = root.GetComponent<DeathExplosion>();
                if (fx == null) fx = root.AddComponent<DeathExplosion>();
                var so = new SerializedObject(fx);
                so.FindProperty("effectPrefab").objectReferenceValue = effect;
                so.FindProperty("scale").floatValue = scale;
                so.FindProperty("offset").vector3Value = offset;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>
        /// Turns a flat front shield into a see-through capsule around the whole enemy (same EnergyShield logic:
        /// blocks Pulse shots, an Overload or explosion drops it for a few seconds).
        /// </summary>
        static string ShieldBubble(string prefabName, string shieldName, float height, float radius)
        {
            string path = $"{EnemyFolder}/{prefabName}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var shield = root.transform.Find(shieldName);
                if (shield == null) return "missing";
                var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                shield.GetComponent<MeshFilter>().sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(temp);

                // Capsule collider first, so EnergyShield always has a collider, then drop the old box
                var capsule = shield.GetComponent<CapsuleCollider>();
                if (capsule == null) capsule = shield.gameObject.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.zero; capsule.radius = 0.5f; capsule.height = 2f; capsule.direction = 1;
                var box = shield.GetComponent<BoxCollider>();
                if (box != null) Object.DestroyImmediate(box);

                // Unit capsule (1 wide, 2 tall) scaled to the enemy, resting on the ground
                shield.localPosition = new Vector3(0f, height * 0.5f, 0f);
                shield.localRotation = Quaternion.identity;
                shield.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

                var renderer = shield.GetComponent<Renderer>();
                renderer.sharedMaterial = BubbleMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return "ok";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>Faint cyan, two-sided, transparent glow so the enemy stays visible inside.</summary>
        static Material BubbleMaterial()
        {
            string path = $"{OutFolder}/Shield_Bubble.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Surface", 1f);                    // transparent
            mat.SetFloat("_Blend", 0f);                      // alpha
            mat.SetFloat("_Cull", 0f);                       // both sides, so the back of the bubble shows too
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(0.3f, 0.85f, 1f, 0.14f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.1f, 0.45f, 0.9f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ animation import

        static readonly string[] Looping = { "Rifle Idle", "Rifle Walk", "Run Forward", "Firing Rifle", "Sword And Shield Idle", "Sword And Shield Walk", "Sitting Idle" };

        /// <summary>Mixamo FBX files as Humanoid clips, played in place (root motion baked), loops set.</summary>
        static Dictionary<string, AnimationClip> ImportMixamo()
        {
            var result = new Dictionary<string, AnimationClip>();
            foreach (var path in Directory.GetFiles(MixamoFolder, "*.fbx").Select(p => p.Replace('\\', '/')))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.SaveAndReimport();
                }
                var settings = importer.defaultClipAnimations;
                foreach (var c in settings)
                {
                    c.name = name;
                    c.loopTime = Looping.Contains(name);
                    c.lockRootRotation = true;
                    c.keepOriginalOrientation = true;
                    c.lockRootHeightY = true;
                    c.keepOriginalPositionY = true;
                    // Not baked: the NavMeshAgent moves the enemy and the Animator drops root motion, so clips that
                    // weren't downloaded "In Place" stay in place instead of drifting and snapping back each loop
                    c.lockRootPositionXZ = false;
                }
                importer.clipAnimations = settings;
                importer.SaveAndReimport();
                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview"));
                if (clip != null) result[name] = clip;
            }
            return result;
        }

        static Avatar MakeHumanoid(string fbx)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            var avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new System.InvalidOperationException($"{Path.GetFileName(fbx)} did not make a valid humanoid avatar.");
            return avatar;
        }

        /// <summary>Speed blend (idle / walk / run), Shoot bool, Hit and Die triggers – the parameters EnemyAI drives.</summary>
        static AnimatorController RobotController(string name, AnimationClip idle, AnimationClip walk, AnimationClip run,
            AnimationClip shoot, AnimationClip hit, AnimationClip die)
        {
            string path = $"{OutFolder}/{name}.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(walk, 1.8f);
            tree.AddChild(run, 4f);
            sm.defaultState = locomotion;

            var shootState = sm.AddState("Shoot");
            shootState.motion = shoot;
            var toShoot = locomotion.AddTransition(shootState);
            toShoot.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            toShoot.hasExitTime = false; toShoot.duration = 0.15f;
            var fromShoot = shootState.AddTransition(locomotion);
            fromShoot.AddCondition(AnimatorConditionMode.IfNot, 0, "Shoot");
            fromShoot.hasExitTime = false; fromShoot.duration = 0.2f;

            var hitState = sm.AddState("Hit");
            hitState.motion = hit;
            var toHit = sm.AddAnyStateTransition(hitState);
            toHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            toHit.hasExitTime = false; toHit.duration = 0.05f; toHit.canTransitionToSelf = false;
            var fromHit = hitState.AddTransition(locomotion);
            fromHit.hasExitTime = true; fromHit.exitTime = 0.6f; fromHit.duration = 0.2f;

            var dieState = sm.AddState("Die");
            dieState.motion = die;
            var toDie = sm.AddAnyStateTransition(dieState);
            toDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            toDie.hasExitTime = false; toDie.duration = 0.1f; toDie.canTransitionToSelf = false;
            return controller;
        }

        // ------------------------------------------------------------------ robots

        static string DressRobot(string prefabName, string robotPrefab, Avatar avatar, AnimatorController controller, float height,
            string visor, string[] hide, HumanBodyBones muzzleBone, string cannon, float scale = 1f)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(robotPrefab);
            if (source == null) return "missing model";
            string path = $"{EnemyFolder}/{prefabName}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveOldModel(root);
                Hide(root, hide);
                // Whole-prefab scale (colliders, weak point, muzzle, NavMeshAgent); the model is then fitted in world units.
                root.transform.localScale = Vector3.one * scale;
                height *= scale;

                var model = Spawn(source, root.transform);
                foreach (var col in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // the visor weak point rides the head bone
                FitHeight(model, root.transform, height);
                animator.Rebind(); // bone lookups need the humanoid avatar bound to the hierarchy

                // Visor weak point onto the face, following the head bone
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var visorT = root.transform.Find(visor);
                if (head != null && visorT != null)
                {
                    float faceForward = height * 0.07f, faceUp = height * 0.035f;
                    visorT.position = head.position + root.transform.forward * faceForward + Vector3.up * faceUp;
                    visorT.rotation = root.transform.rotation;
                    visorT.SetParent(head, true);
                }

                // Muzzle (and the Aegis shoulder cannon) onto a bone so shots leave the robot's body
                var bone = animator.GetBoneTransform(muzzleBone);
                if (bone == null) bone = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (bone == null) bone = animator.GetBoneTransform(HumanBodyBones.Spine); // Engie's rig has no chest bones
                var muzzle = root.transform.Find("Muzzle");
                var cannonT = cannon != null ? root.transform.Find(cannon) : null;
                if (bone != null && cannonT != null)
                {
                    cannonT.position = bone.position + root.transform.right * height * 0.17f + Vector3.up * height * 0.1f;
                    cannonT.rotation = root.transform.rotation;
                    cannonT.SetParent(bone, true);
                    var cannonRenderer = cannonT.GetComponent<Renderer>();
                    if (cannonRenderer != null) cannonRenderer.enabled = false; // no block on the shoulder: it stays only as the invisible mount the shots leave from
                }
                if (bone != null && muzzle != null)
                {
                    var anchor = cannonT != null ? cannonT : bone;
                    muzzle.position = anchor.position + root.transform.forward * (cannonT != null ? 0.4f : 0.15f);
                    muzzle.rotation = root.transform.rotation;
                    muzzle.SetParent(anchor, true);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return $"ok (head bone {(head != null ? head.name : "none")})";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ------------------------------------------------------------------ drone

        static string DressStinger()
        {
            var fbx = $"{DroneFolder}/source/Drone.FBX";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (source == null) return "missing model";

            // Rotor/idle animation from the FBX, looping
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            var settings = importer.defaultClipAnimations;
            foreach (var c in settings) c.loopTime = true;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().FirstOrDefault(a => !a.name.StartsWith("__preview"));

            string ctrlPath = $"{OutFolder}/ReconStinger.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            foreach (var p in new[] { "Speed", "Shoot", "Hit", "Die" }) // EnemyAI sets these; unused by the drone
                controller.AddParameter(p, p == "Speed" ? AnimatorControllerParameterType.Float
                    : p == "Shoot" ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Trigger);
            if (clip != null) controller.layers[0].stateMachine.AddState("Hover").motion = clip;

            string tex = $"{DroneFolder}/textures";
            var body = LitMaterial("Drone_Body", $"{tex}/Drone_diff.jpeg", $"{tex}/Drone_normal.jpeg", 0.55f, 0.4f,
                emission: $"{tex}/Drone_emissive.jpeg", emissionColor: new Color(0.6f, 1.6f, 2.2f));
            var flame = LitMaterial("Drone_Thruster", $"{tex}/Fire_Dif.jpeg", null, 0f, 0f,
                emission: $"{tex}/Fire_Dif.jpeg", emissionColor: new Color(0.8f, 1.6f, 2.5f));

            string path = $"{EnemyFolder}/ReconStinger.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveOldModel(root);
                Hide(root, "Body", "Rotor_L", "Rotor_R");
                var model = Spawn(source, root.transform);
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = mats[i] != null && mats[i].name.Contains("Fire") ? flame : body;
                    r.sharedMaterials = mats;
                }
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                // About 1 m across, centered on the hover point
                var b = Bounds(model);
                Scale(model, root.transform, 1.0f / Mathf.Max(b.size.x, b.size.z));
                b = Bounds(model);
                model.transform.position += root.transform.position - b.center;
                b = Bounds(model);

                // Eye weak point on the drone's front
                var eye = root.transform.Find("Eye_WeakPoint");
                if (eye != null) eye.position = b.center + root.transform.forward * (b.extents.z * 0.95f);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return "ok";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ------------------------------------------------------------------ Titan

        static string DressTitan()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{TitanFolder}/source/Mech_non_P.fbx");
            if (source == null) return "missing model";
            string tex = $"{TitanFolder}/textures";
            var materials = new Dictionary<string, Material>();
            foreach (var part in new[] { "body", "hands", "legs" })
            {
                var mat = LitMaterial($"Titan_{part}", $"{tex}/LP_{part}_BaseColor_2k.jpg", $"{tex}/LP_{part}_Normal_2k.jpg", 1f, 1f,
                    emission: File.Exists($"{tex}/LP_non_P_{part}_Emissive_2k.png") ? $"{tex}/LP_non_P_{part}_Emissive_2k.png" : null,
                    emissionColor: Color.white * 1.5f);
                var metalSmooth = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/LP_{part}_MetalSmooth_2k.png");
                if (metalSmooth != null)
                {
                    mat.SetTexture("_MetallicGlossMap", metalSmooth);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                var ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{tex}/LP_non_P_{part}_AO_2k.jpg");
                if (ao != null)
                {
                    mat.SetTexture("_OcclusionMap", ao);
                    mat.EnableKeyword("_OCCLUSIONMAP");
                }
                materials[part] = mat;
            }

            string path = $"{EnemyFolder}/Unit00_Titan.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveOldModel(root);
                Hide(root, "Body", "Head", "Eye");
                var model = Spawn(source, root.transform);
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] != null && materials.TryGetValue(mats[i].name.Split(' ')[0].ToLower(), out var m)) mats[i] = m;
                    r.sharedMaterials = mats;
                }
                foreach (var col in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
                FitHeight(model, root.transform, 5.6f);
                model.AddComponent<MechWalkBob>();
                string rig = TitanRigBuilder.Rig(model, root.transform); // split the rigid pieces into body and legs for the walk animation
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return $"ok ({rig})";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>
        /// One-time: resizes the Titan's 8K PNGs to 2K (JPG for color/normal/AO, PNG for emissive), packs metallic
        /// and roughness into a URP metallic/smoothness map, and moves the 8K originals to _RemovedAssets/.
        /// </summary>
        static string ShrinkTitanTextures()
        {
            string tex = $"{TitanFolder}/textures";
            if (File.Exists($"{tex}/LP_body_MetalSmooth_2k.png")) return "already 2K";
            int written = 0;
            var originals = new List<string>();
            foreach (var part in new[] { "body", "hands", "legs" })
            {
                written += Resize($"{tex}/LP_{part}_BaseColor.png", $"{tex}/LP_{part}_BaseColor_2k.jpg", originals);
                written += Resize($"{tex}/LP_{part}_Normal.png", $"{tex}/LP_{part}_Normal_2k.jpg", originals);
                written += Resize($"{tex}/LP_non_P_{part}_AO.png", $"{tex}/LP_non_P_{part}_AO_2k.jpg", originals);
                written += Resize($"{tex}/LP_non_P_{part}_Emissive.png", $"{tex}/LP_non_P_{part}_Emissive_2k.png", originals);

                var metal = ReadAt2K($"{tex}/LP_non_P_{part}_Metallic.png");
                var rough = ReadAt2K($"{tex}/LP_non_P_{part}_Roughness.png");
                if (metal != null && rough != null)
                {
                    var packed = new Color32[metal.Length];
                    for (int i = 0; i < packed.Length; i++)
                        packed[i] = new Color32(metal[i].r, 0, 0, (byte)(255 - rough[i].r));
                    Write($"{tex}/LP_{part}_MetalSmooth_2k.png", packed, true);
                    originals.Add($"{tex}/LP_non_P_{part}_Metallic.png");
                    originals.Add($"{tex}/LP_non_P_{part}_Roughness.png");
                    written++;
                }
            }
            AssetDatabase.Refresh();

            // Import settings for the new files
            foreach (var path in Directory.GetFiles(tex).Select(p => p.Replace('\\', '/')).Where(p => p.Contains("_2k.")))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;
                bool data = path.Contains("_AO_") || path.Contains("MetalSmooth");
                if (path.Contains("_Normal_")) importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = !data && !path.Contains("_Normal_");
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            // Move the 8K originals out of the project (the backup folder is git-ignored)
            foreach (var path in originals.Distinct())
            {
                string dest = Path.Combine("_RemovedAssets", path);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(path, dest);
                if (File.Exists(path + ".meta"))
                {
                    if (File.Exists(dest + ".meta")) File.Delete(dest + ".meta");
                    File.Move(path + ".meta", dest + ".meta");
                }
            }
            AssetDatabase.Refresh();
            return $"{written} written, {originals.Distinct().Count()} 8K originals moved";
        }

        static int Resize(string source, string destination, List<string> originals)
        {
            var pixels = ReadAt2K(source);
            if (pixels == null) return 0;
            Write(destination, pixels, destination.EndsWith(".png"));
            originals.Add(source);
            return 1;
        }

        /// <summary>Reimports a texture readable and uncompressed at 2048 and returns its pixels.</summary>
        static Color32[] ReadAt2K(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return texture.width == 2048 && texture.height == 2048 ? texture.GetPixels32() : null;
        }

        static void Write(string path, Color32[] pixels, bool png)
        {
            var texture = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, png ? texture.EncodeToPNG() : texture.EncodeToJPG(92));
            Object.DestroyImmediate(texture);
        }

        // ------------------------------------------------------------------ boat

        static string DressBoat()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{BoatFolder}/source/Boat_02.fbx");
            var boat = Object.FindObjectsByType<ExtractionBoat>(FindObjectsInactive.Include).FirstOrDefault();
            if (source == null || boat == null) return "missing";
            var mat = LitMaterial("Boat_Hull", $"{BoatFolder}/textures/T_Boat_BC_02.png", null, 0f, 0.45f);

            // The model sits next to the (scaled) greybox box rather than inside it, so it keeps a uniform scale
            var parent = boat.transform.parent;
            string modelName = "ExtractionBoat_Model";
            var old = parent != null ? parent.Find(modelName) : null;
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var inside = boat.transform.Find(ModelName); // left by an earlier version of this builder
            if (inside != null) Object.DestroyImmediate(inside.gameObject);
            var box = boat.GetComponent<Renderer>();
            box.enabled = true;
            var target = box.bounds;
            box.enabled = false;

            var model = Spawn(source, parent);
            model.name = modelName;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;
            // The model is long along X; the greybox boat is long along Z
            model.transform.rotation = boat.transform.rotation * Quaternion.Euler(0, 90, 0);
            var b = Bounds(model);
            model.transform.localScale *= target.size.z / b.size.z;
            b = Bounds(model);
            model.transform.position += new Vector3(target.center.x - b.center.x, target.min.y - b.min.y, target.center.z - b.center.z);
            return "ok";
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Deletes the previous model, first moving the gameplay parts that were attached to its bones (visor weak
        /// point, muzzle, shoulder cannon) back onto the root so re-running doesn't delete them with it.
        /// </summary>
        static void RemoveOldModel(GameObject root)
        {
            var old = root.transform.Find(ModelName);
            if (old == null) return;
            foreach (var t in old.GetComponentsInChildren<Transform>(true))
                if (t.name == "Visor_WeakPoint" || t.name == "Muzzle" || t.name == "ShoulderCannon" || t.name == "ArmCannon")
                    t.SetParent(root.transform, true);
            Object.DestroyImmediate(old.gameObject);
        }

        static void Hide(GameObject root, params string[] names)
        {
            foreach (var name in names)
            {
                var t = root.transform.Find(name);
                var r = t != null ? t.GetComponent<Renderer>() : null;
                if (r != null) r.enabled = false;
            }
        }

        static GameObject Spawn(GameObject source, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = ModelName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }

        /// <summary>Uniform world scale to the given height, feet on the root, centered on it.</summary>
        static void FitHeight(GameObject model, Transform root, float height)
        {
            var b = Bounds(model);
            Scale(model, root, height / b.size.y);
            b = Bounds(model);
            model.transform.position += new Vector3(root.position.x - b.center.x, root.position.y - b.min.y, root.position.z - b.center.z);
        }

        /// <summary>Multiplies the model's world scale by factor, undoing any non-uniform parent scale.</summary>
        static void Scale(GameObject model, Transform parent, float factor)
        {
            var world = Vector3.Scale(model.transform.localScale, parent.lossyScale) * factor;
            var p = parent.lossyScale;
            model.transform.localScale = new Vector3(world.x / p.x, world.y / p.y, world.z / p.z);
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        static Material LitMaterial(string name, string baseMap, string normalMap, float metallic, float smoothness,
            string emission = null, Color emissionColor = default)
        {
            string path = $"{OutFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMap));
            mat.SetColor("_BaseColor", Color.white);
            if (normalMap != null)
            {
                var importer = AssetImporter.GetAtPath(normalMap) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalMap));
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission != null)
            {
                mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(emission));
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
