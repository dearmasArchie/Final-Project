using SignalLost.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Builds Partner A's Phase 1 assets: the Pulse bolt prefab, the Player prefab (greybox gun model),
    /// and the Sandbox_A test scene with shooting dummies. Also swaps the DebugPlayer in both gameplay
    /// scenes for the real Player prefab.
    /// </summary>
    public static class PlayerPrefabBuilder
    {
        public const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";
        const string BoltPath = "Assets/_Project/Prefabs/PulseBolt.prefab";
        const string MaterialsFolder = "Assets/_Project/Materials";
        const string SandboxPath = "Assets/_Project/Scenes/Sandbox_A.unity";
        static readonly string[] GameplayScenes =
        {
            "Assets/_Project/Scenes/IslandExterior.unity", "Assets/_Project/Scenes/UndergroundFacility.unity"
        };

        [MenuItem("Tools/Signal Lost/Build Player Prefab")]
        public static GameObject BuildPlayer()
        {
            var bolt = BuildBolt();
            int playerLayer = LayerMask.NameToLayer("Player");
            int notPlayer = ~(1 << playerLayer);

            var root = new GameObject("Player");
            root.tag = "Player";
            root.layer = playerLayer;

            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.4f;

            var camGo = new GameObject("CameraPivot", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            camGo.GetComponent<Camera>().nearClipPlane = 0.05f;
            var cam = camGo.transform;
            // Mouse look rotates the pivot; the shake only offsets its position, so they never fight
            camGo.AddComponent<CameraShake>();
            camGo.GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = true;

            // Greybox gun: replaced by the Sci-fi Pistol model in the art pass.
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "ArcPistol_Model";
            Object.DestroyImmediate(gun.GetComponent<Collider>());
            gun.transform.SetParent(cam, false);
            gun.transform.localPosition = new Vector3(0.22f, -0.2f, 0.45f);
            gun.transform.localScale = new Vector3(0.08f, 0.12f, 0.3f);
            gun.GetComponent<Renderer>().sharedMaterial = Mat("ArcPistol_Greybox", new Color(0.25f, 0.28f, 0.32f), Color.black);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(cam, false);
            muzzle.localPosition = new Vector3(0.22f, -0.17f, 0.62f);

            var beam = new GameObject("OverloadBeam").AddComponent<LineRenderer>();
            beam.transform.SetParent(root.transform, false);
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.widthMultiplier = 0.08f;
            beam.shadowCastingMode = ShadowCastingMode.Off;
            var beamColor = new Color(0.5f, 0.95f, 1f);
            beam.sharedMaterial = Mat("OverloadBeam", beamColor, beamColor * 6f);
            var lightning = beam.gameObject.AddComponent<LightningLine>();
            var lso = new SerializedObject(lightning);
            lso.FindProperty("jitter").floatValue = 0.12f;
            lso.FindProperty("segments").intValue = 16;
            lso.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<PlayerInputReader>();
            var fpc = root.AddComponent<FirstPersonController>();
            root.AddComponent<PlayerHealth>();
            var interaction = root.AddComponent<PlayerInteraction>();
            var pistol = root.AddComponent<ArcPistol>();
            root.AddComponent<PlayerPersistence>();

            Set(fpc, "cameraPivot", cam);
            Set(interaction, "cameraPivot", cam);
            SetMask(interaction, "mask", notPlayer);
            Set(pistol, "cameraPivot", cam);
            Set(pistol, "muzzle", muzzle);
            Set(pistol, "model", gun);
            Set(pistol, "beamLine", beam);
            Set(pistol, "pulsePrefab", bolt.GetComponent<Projectile>());
            SetMask(pistol, "hitMask", notPlayer);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[Signal Lost] Player prefab saved to {PlayerPath}");
            return prefab;
        }

        static GameObject BuildBolt()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PulseBolt";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.08f;
            var cyan = new Color(0.3f, 0.9f, 1f);
            var mat = Mat("PulseBolt", cyan, cyan * 4f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.06f;
            trail.widthMultiplier = 0.04f;
            trail.sharedMaterial = mat;
            trail.shadowCastingMode = ShadowCastingMode.Off;

            go.AddComponent<Projectile>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, BoltPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        [MenuItem("Tools/Signal Lost/Swap DebugPlayer For Player")]
        public static void SwapIntoGameplayScenes()
        {
            EditorSceneManager.SaveOpenScenes();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) ?? BuildPlayer();
            foreach (var path in GameplayScenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var debug = Object.FindFirstObjectByType<DebugPlayer>();
                if (debug == null) continue;

                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                player.transform.SetPositionAndRotation(debug.transform.position, debug.transform.rotation);
                Object.DestroyImmediate(debug.gameObject);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Signal Lost] DebugPlayer replaced by the Player prefab in both gameplay scenes.");
        }

        [MenuItem("Tools/Signal Lost/Build Sandbox_A")]
        public static void BuildSandbox()
        {
            EditorSceneManager.SaveOpenScenes();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) ?? BuildPlayer();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null) Object.DestroyImmediate(mainCam);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(4, 1, 4);
            floor.layer = LayerMask.NameToLayer("Ground");
            floor.GetComponent<Renderer>().sharedMaterial = Mat("Sandbox_Floor", new Color(0.4f, 0.4f, 0.4f), Color.black);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "BackWall";
            wall.transform.position = new Vector3(0, 2, 20);
            wall.transform.localScale = new Vector3(20, 4, 0.5f);

            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HUDPrefabBuilder.PrefabPath);
            if (hud != null) PrefabUtility.InstantiatePrefab(hud);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = new Vector3(0, 0, -6);
            new GameObject("DebugGrantPistol").AddComponent<DebugGrantPistol>();

            var body = Mat("Dummy_Body", new Color(0.55f, 0.5f, 0.45f), Color.black);
            var eye = Mat("Dummy_WeakPoint", new Color(1f, 0.2f, 0.15f), new Color(2f, 0.2f, 0.1f));
            for (int i = 0; i < 3; i++)
                Dummy(new Vector3(-6 + i * 6, 0, 4 + i * 4), body, eye);

            EditorSceneManager.SaveScene(scene, SandboxPath);
            Debug.Log($"[Signal Lost] Sandbox scene saved to {SandboxPath}");
        }

        static void Dummy(Vector3 position, Material body, Material eye)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "TestDummy";
            go.layer = LayerMask.NameToLayer("Enemy");
            go.transform.position = position + Vector3.up;
            go.GetComponent<Renderer>().sharedMaterial = body;
            var health = go.AddComponent<Health>();
            var so = new SerializedObject(health);
            so.FindProperty("destroyOnDeath").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<TestDummy>();

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "WeakPoint_Visor";
            head.layer = go.layer;
            head.tag = "WeakPoint";
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 0.75f, 0.25f);
            head.transform.localScale = Vector3.one * 0.35f;
            head.GetComponent<Renderer>().sharedMaterial = eye;
            head.AddComponent<WeakPoint>();
        }

        // ------------------------------------------------------------------ helpers

        static void Set(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetMask(Object target, string property, int mask)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).intValue = mask;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material Mat(string name, Color color, Color emission)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            if (emission.maxColorComponent > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
