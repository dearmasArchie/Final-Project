using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 2: builds greybox enemy, pickup and prop prefabs, then places the Phase 2 gameplay
    /// (briefing, armory, road enemies, doors, Wing A lasers + breaker puzzle, Wing B water + valve + lockdown)
    /// under a root object named "Gameplay" in each scene. Re-running rebuilds "Gameplay" only; the greybox is untouched.
    /// Positions match the greybox layout from GreyboxBuilder.
    /// </summary>
    public static class Phase2ContentBuilder
    {
        const string RootName = "Gameplay";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string DataFolder = "Assets/_Project/Data";
        const string MatFolder = "Assets/_Project/Materials";
        const string Island = "Assets/_Project/Scenes/IslandExterior.unity";
        const string Bunker = "Assets/_Project/Scenes/UndergroundFacility.unity";
        const string Sandbox = "Assets/_Project/Scenes/Sandbox_A.unity";

        static GameObject enforcer, stinger, battery, medkit, fuse, pistolPickup, barrel, crate;
        internal static int enemyLayer, interactLayer, notEnemy;

        [MenuItem("Tools/Signal Lost/Build Phase 2 Content")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            enemyLayer = LayerMask.NameToLayer("Enemy");
            interactLayer = LayerMask.NameToLayer("Interactable");
            notEnemy = ~(1 << enemyLayer);

            HUDPrefabBuilder.Build();
            PlayerPrefabBuilder.BuildPlayer();
            BuildPrefabs();
            PropArtBuilder.DressPrefabs(); // keep the Poly Haven models on the rebuilt crate, barrel and medkit
            EnemyArtBuilder.DressEnemies(); // and the robot, drone and Titan models on the rebuilt enemies

            PopulateIsland();
            PopulateBunker();
            PopulateSandbox();
            AssetDatabase.SaveAssets(); // the painted bunker path lives in the terrain asset
            EditorSceneManager.OpenScene(Island);
            Debug.Log("[Signal Lost] Phase 2 content built.");
        }

        // ================================================================== prefabs

        static void BuildPrefabs()
        {
            EnsureFolder($"{PrefabFolder}/Enemies");
            EnsureFolder($"{PrefabFolder}/Pickups");
            EnsureFolder($"{PrefabFolder}/Props");
            EnsureFolder(DataFolder);

            battery = SavePickup("BatteryClip", PrimitiveType.Cube, new Vector3(0.2f, 0.3f, 0.12f),
                Mat("Pickup_Battery", new Color(0.2f, 0.8f, 1f), new Color(0.3f, 1.5f, 2f)), Pickup.Kind.BatteryClip, 12);
            medkit = SavePickup("Medkit", PrimitiveType.Cube, new Vector3(0.4f, 0.25f, 0.25f),
                Mat("Pickup_Medkit", new Color(0.95f, 0.95f, 0.95f), new Color(1.2f, 0.2f, 0.2f)), Pickup.Kind.Medkit, 35);
            fuse = SavePickup("OverrideFuse", PrimitiveType.Cylinder, new Vector3(0.14f, 0.12f, 0.14f),
                Mat("Pickup_Fuse", new Color(1f, 0.8f, 0.1f), new Color(3f, 2.2f, 0.3f)), Pickup.Kind.Fuse, 0);
            pistolPickup = SavePickup("ArcPistolPickup", PrimitiveType.Cube, new Vector3(0.08f, 0.12f, 0.3f),
                Mat("Pickup_Pistol", new Color(0.25f, 0.28f, 0.32f), new Color(0.2f, 0.8f, 1f)), Pickup.Kind.ArcPistol, 0);

            barrel = SaveBarrel();
            crate = SaveCrate();

            var laser = SaveBolt("EnemyLaser", new Color(1f, 0.15f, 0.1f), 25f, 3f);
            var enforcerStats = Stats("Stats_Enforcer", s =>
            {
                s.patrolSpeed = 2f; s.chaseSpeed = 3.5f; s.attackRange = 10f;
                s.sideStepWhenHit = true; s.advanceWhileAttacking = false;
            });
            var stingerStats = Stats("Stats_ReconStinger", s =>
            {
                s.patrolSpeed = 3f; s.chaseSpeed = 6f; s.attackRange = 4f;
                s.zigZagAmplitude = 2.5f; s.advanceWhileAttacking = true; s.preferredAttackDistance = 2.5f;
                s.sideStepWhenHit = false;
            });
            enforcer = SaveEnforcer(enforcerStats, laser);
            stinger = SaveStinger(stingerStats);
        }

        static GameObject SavePickup(string name, PrimitiveType shape, Vector3 size, Material mat, Pickup.Kind kind, int amount)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            go.layer = interactLayer;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var pickup = go.AddComponent<Pickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("amount").intValue = amount;
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(go, $"{PrefabFolder}/Pickups/{name}.prefab");
        }

        static GameObject SaveBarrel()
        {
            var root = new GameObject("ExplosiveBarrel");
            var body = Child(root, "Body", PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0), new Vector3(0.6f, 0.45f, 0.6f),
                Mat("Barrel_Red", new Color(0.8f, 0.1f, 0.05f), Color.black));
            Child(root, "HazardStripe", PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0), new Vector3(0.62f, 0.05f, 0.62f),
                Mat("Barrel_Stripe", new Color(1f, 0.85f, 0.1f), new Color(0.6f, 0.5f, 0f)), keepCollider: false);
            root.AddComponent<ExplosiveBarrel>();
            body.name = "Body";
            return SavePrefab(root, $"{PrefabFolder}/Props/ExplosiveBarrel.prefab");
        }

        static GameObject SaveCrate()
        {
            var root = new GameObject("SupplyCrate");
            Child(root, "Body", PrimitiveType.Cube, new Vector3(0, 0.4f, 0), Vector3.one * 0.8f,
                Mat("Crate_Wood", new Color(0.55f, 0.38f, 0.2f), Color.black));
            var d = root.AddComponent<Destructible>();
            var so = new SerializedObject(d);
            so.FindProperty("batteryPrefab").objectReferenceValue = battery;
            so.FindProperty("medkitPrefab").objectReferenceValue = medkit;
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, $"{PrefabFolder}/Props/SupplyCrate.prefab");
        }

        internal static Projectile SaveBolt(string name, Color color, float speed, float lifetime)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.09f;
            var mat = Mat(name, color, color * 4f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.08f;
            trail.widthMultiplier = 0.05f;
            trail.sharedMaterial = mat;
            var p = go.AddComponent<Projectile>();
            var so = new SerializedObject(p);
            so.FindProperty("speed").floatValue = speed;
            so.FindProperty("lifetime").floatValue = lifetime;
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(go, $"{PrefabFolder}/Enemies/{name}.prefab").GetComponent<Projectile>();
        }

        static GameObject SaveEnforcer(EnemyStats stats, Projectile laser)
        {
            var root = new GameObject("Enforcer");
            root.layer = enemyLayer;
            var metal = Mat("Enemy_Metal", new Color(0.35f, 0.37f, 0.4f), Color.black);
            var sensor = Mat("Enemy_Sensor", new Color(0.2f, 0.5f, 1f), new Color(0.8f, 2f, 4f));

            Child(root, "Body", PrimitiveType.Capsule, new Vector3(0, 0.9f, 0), new Vector3(0.7f, 0.9f, 0.7f), metal);
            Child(root, "Head", PrimitiveType.Cube, new Vector3(0, 1.62f, 0), new Vector3(0.34f, 0.3f, 0.34f), metal, keepCollider: false);
            var visor = Child(root, "Visor_WeakPoint", PrimitiveType.Cube, new Vector3(0, 1.65f, 0.17f), new Vector3(0.32f, 0.1f, 0.08f), sensor);
            visor.tag = "WeakPoint";
            visor.AddComponent<WeakPoint>();
            Child(root, "ArmCannon", PrimitiveType.Cube, new Vector3(0.38f, 1.2f, 0.25f), new Vector3(0.12f, 0.12f, 0.55f), metal, keepCollider: false);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0.38f, 1.2f, 0.6f);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.8f;
            agent.speed = stats.patrolSpeed;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.5f;
            SetHealth(root.AddComponent<Health>(), 100f);

            var attack = root.AddComponent<RangedBurstAttack>();
            var aso = new SerializedObject(attack);
            aso.FindProperty("muzzle").objectReferenceValue = muzzle;
            aso.FindProperty("boltPrefab").objectReferenceValue = laser;
            aso.FindProperty("hitMask").intValue = notEnemy;
            aso.ApplyModifiedPropertiesWithoutUndo();

            SetupAI(root.AddComponent<EnemyAI>(), stats, visor.transform, visor.GetComponent<Renderer>());
            return SavePrefab(root, $"{PrefabFolder}/Enemies/Enforcer.prefab");
        }

        static GameObject SaveStinger(EnemyStats stats)
        {
            var root = new GameObject("ReconStinger");
            root.layer = enemyLayer;
            var metal = Mat("Enemy_DroneShell", new Color(0.25f, 0.27f, 0.3f), Color.black);
            var sensor = Mat("Enemy_Sensor", new Color(0.2f, 0.5f, 1f), new Color(0.8f, 2f, 4f));

            Child(root, "Body", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.6f, metal);
            Child(root, "Rotor_L", PrimitiveType.Cylinder, new Vector3(-0.42f, 0.1f, 0), new Vector3(0.3f, 0.02f, 0.3f), metal, keepCollider: false);
            Child(root, "Rotor_R", PrimitiveType.Cylinder, new Vector3(0.42f, 0.1f, 0), new Vector3(0.3f, 0.02f, 0.3f), metal, keepCollider: false);
            var eye = Child(root, "Eye_WeakPoint", PrimitiveType.Sphere, new Vector3(0, 0, 0.27f), Vector3.one * 0.2f, sensor);
            eye.tag = "WeakPoint";
            eye.AddComponent<WeakPoint>();

            var beam = new GameObject("ShockBeam").AddComponent<LineRenderer>();
            beam.transform.SetParent(root.transform, false);
            beam.positionCount = 2;
            beam.widthMultiplier = 0.05f;
            var beamColor = new Color(0.6f, 0.9f, 1f);
            beam.sharedMaterial = Mat("ShockBeam", beamColor, beamColor * 5f);
            beam.shadowCastingMode = ShadowCastingMode.Off;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 0.6f;
            agent.baseOffset = 1.3f; // hovers at chest height
            agent.speed = stats.patrolSpeed;
            agent.angularSpeed = 540f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 0.3f;
            SetHealth(root.AddComponent<Health>(), 40f);

            var attack = root.AddComponent<ShockBeamAttack>();
            var aso = new SerializedObject(attack);
            aso.FindProperty("emitter").objectReferenceValue = eye.transform;
            aso.FindProperty("beam").objectReferenceValue = beam;
            aso.FindProperty("sightMask").intValue = notEnemy;
            aso.ApplyModifiedPropertiesWithoutUndo();

            SetupAI(root.AddComponent<EnemyAI>(), stats, eye.transform, eye.GetComponent<Renderer>());
            return SavePrefab(root, $"{PrefabFolder}/Enemies/ReconStinger.prefab");
        }

        internal static void SetupAI(EnemyAI ai, EnemyStats stats, Transform eye, Renderer sensor)
        {
            var so = new SerializedObject(ai);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.FindProperty("eye").objectReferenceValue = eye;
            so.FindProperty("sensorRenderer").objectReferenceValue = sensor;
            so.FindProperty("sightMask").intValue = notEnemy;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetHealth(Health health, float max)
        {
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = max;
            so.FindProperty("destroyOnDeath").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static EnemyStats Stats(string name, System.Action<EnemyStats> configure)
        {
            var path = $"{DataFolder}/{name}.asset";
            var stats = AssetDatabase.LoadAssetAtPath<EnemyStats>(path);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStats>();
                configure(stats);
                AssetDatabase.CreateAsset(stats, path);
            }
            return stats;
        }

        // ================================================================== island

        static void PopulateIsland()
        {
            var scene = EditorSceneManager.OpenScene(Island);
            var root = ResetRoot();
            var terrain = Terrain.activeTerrain;
            Vector3 At(float x, float z, float up = 0f) => new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, z)) + up, z);

            new GameObject("ObjectiveManager", typeof(ObjectiveManager)).transform.SetParent(root.transform);

            // Safe room: Dr. Reyes, armory door, Arc Pistol
            var safe = Group(root, "SafeRoom");
            var door = MakeDoor(safe, "ArmoryDoor", At(86, 45), new Vector3(0.2f, 2.6f, 1.6f), new Vector3(0, 0, 1.7f),
                lockedReason: "Locked – enter the code on the keypad", startsLocked: true,
                unlockedWhenCleared: CodeKeypad.UnlockedId, alongZ: true);
            // The keypad on the safe-room side of the wall takes the code; the door itself only shows why it's locked.
            foreach (Transform child in door.transform)
                if (child.name == "Keypad" && child.position.x > door.transform.position.x)
                    SetRef(child.gameObject.AddComponent<CodeKeypad>(), "door", door);

            var reyes = Prim(safe, "Dr_Reyes", PrimitiveType.Capsule, At(95.5f, 47, 0.7f), new Vector3(0.7f, 0.7f, 0.7f),
                Mat("NPC_Reyes", new Color(0.85f, 0.85f, 0.8f), Color.black));
            reyes.transform.rotation = Quaternion.Euler(0, 220, 0);
            reyes.layer = interactLayer;
            var dialogue = reyes.AddComponent<DialogueSource>();
            var dso = new SerializedObject(dialogue);
            SetStrings(dso.FindProperty("lines"),
                "You made it... good. OVERSEER took over the drones and robot guards after the lightning strike. It thinks we're intruders.",
                "It also started a Purge Protocol. In twenty minutes the defense grid fires on everything left on this island.",
                "The radio tower can call a rescue boat, but it needs three override fuses. Two are in the underground utility wings, one at the tower.",
                "My leg's broken, so it has to be you. The armory has the Arc Pistol, but it's keypad-locked and I never learned the code.",
                "The technicians kept it in the old maintenance bunker, far to the northwest – follow the dirt path. They never wrote codes down plainly, so look carefully. No bots patrol out there.");
            SetStrings(dso.FindProperty("repeatLines"),
                "The armory code is somewhere in the maintenance bunker to the northwest. Once you've worked it out, use the keypad by the armory door.");
            dso.FindProperty("persistentId").stringValue = "Briefing";
            dso.FindProperty("objectiveOnFinish").stringValue = "Find the armory code in the maintenance bunker northwest of the safe room";
            dso.ApplyModifiedPropertiesWithoutUndo();

            BuildCodeBunker(Group(root, "MaintenanceBunker"), terrain);

            Prim(safe, "ArmoryTable", PrimitiveType.Cube, At(83.5f, 45, 0.45f), new Vector3(0.6f, 0.9f, 1.2f),
                Mat("Prop_Table", new Color(0.4f, 0.4f, 0.42f), Color.black));
            PlacePickup(safe, pistolPickup, At(83.5f, 45, 1.05f), "Pickup_ArcPistol", null);

            // Elevator needs the Arc Pistol and main power (generator bunker). Red/green light above the panel.
            SetPortalRequirement("ElevatorPanel_Down", ScenePortal.Requirement.ArcPistol);
            var panelGo = GameObject.Find("ElevatorPanel_Down");
            if (panelGo != null)
            {
                var status = Prim(safe.parent, "ElevatorStatusLight", PrimitiveType.Sphere, panelGo.transform.position + Vector3.up * 0.55f,
                    Vector3.one * 0.15f, Mat("Elevator_StatusLight", new Color(1f, 0.15f, 0.1f), new Color(3f, 0.4f, 0.3f)));
                Object.DestroyImmediate(status.GetComponent<Collider>());
                var portalSo = new SerializedObject(panelGo.GetComponent<ScenePortal>());
                portalSo.FindProperty("requiredClearedId").stringValue = PowerSwitch.PoweredId;
                portalSo.FindProperty("missingClearedMessage").stringValue = "No power – restore it at the generator bunker";
                portalSo.FindProperty("statusLight").objectReferenceValue = status.GetComponent<Renderer>();
                portalSo.ApplyModifiedPropertiesWithoutUndo();

                var noPower = new GameObject("Radio_NoPower");
                noPower.transform.SetParent(safe.parent, false);
                noPower.transform.position = panelGo.transform.position + new Vector3(-2.3f, 0.2f, -3.5f);
                noPower.AddComponent<BoxCollider>().size = new Vector3(7f, 4f, 7f);
                var noPowerRadio = noPower.AddComponent<RadioTrigger>();
                var npso = new SerializedObject(noPowerRadio);
                npso.FindProperty("line").stringValue = "The elevator's dead – OVERSEER cut the power. Throw the main switch in the generator bunker, east of the road. It'll be guarded.";
                npso.FindProperty("persistentId").stringValue = "Radio_NoPower";
                npso.FindProperty("onlyIfNotCleared").stringValue = PowerSwitch.PoweredId;
                npso.ApplyModifiedPropertiesWithoutUndo();
            }

            BuildGeneratorBunker(Group(root, "GeneratorBunker"), terrain);

            // Island road: a lone Enforcer to learn on, then a squad by a barrel
            var road = Group(root, "Road");
            // Far enough from the safe room door (>15 m) that the unarmed player can reach the bunker unseen.
            PlaceEnemy(road, enforcer, At(102, 76), "Island_E1", At(103, 73), At(99, 81));
            PlaceEnemy(road, enforcer, At(114, 88), "Island_E2", At(110, 84), At(119, 93));
            PlaceEnemy(road, stinger, At(116, 96), "Island_S1", At(116, 96), At(122, 90));
            PlaceEnemy(road, stinger, At(108, 92), "Island_S2", At(108, 92), At(104, 86));
            Place(road, barrel, At(112.5f, 89.5f));
            Place(road, barrel, At(116.5f, 86f));
            Place(road, crate, At(104, 74));
            Place(road, crate, At(121, 99));
            Place(road, crate, At(97, 55));
            PlacePickup(road, medkit, At(123, 101, 0.4f), "Island_Medkit1", null);

            EnsurePlayer(At(93, 43));
            EditorSceneManager.SaveScene(scene);
        }

        // ================================================================== maintenance bunker (armory code puzzle)

        static readonly string[] LockerColorNames = { "Blue", "Red", "Green", "Yellow" };
        static readonly Color[] LockerColors =
        {
            new Color(0.15f, 0.3f, 0.75f), new Color(0.7f, 0.12f, 0.1f), new Color(0.15f, 0.55f, 0.2f), new Color(0.85f, 0.7f, 0.1f)
        };

        /// <summary>Where the maintenance bunker stands (island X/Z). The door faces east, toward the safe room.</summary>
        static readonly Vector2 CodeBunkerCenter = new Vector2(37.36f, 131.03f); // placed by hand in the Editor

        /// <summary>
        /// Small concrete bunker ~85 m northwest of the safe room. Four colored lockers show the digits, the west wall
        /// shows the color order, decoys mislead, and a light switch reveals the hidden glow-paint digit.
        /// </summary>
        static void BuildCodeBunker(Transform group, Terrain terrain)
        {
            float cx = CodeBunkerCenter.x, cz = CodeBunkerCenter.y;
            float gy = terrain.SampleHeight(new Vector3(cx, 0, cz));
            Vector3 V(float dx, float dy, float dz) => new Vector3(cx + dx, gy + dy, cz + dz);
            var concrete = Mat("Bunker_Concrete", new Color(0.55f, 0.55f, 0.52f), Color.black);

            Prim(group, "Floor", PrimitiveType.Cube, V(0, -0.05f, 0), new Vector3(8.6f, 0.2f, 6.6f), concrete);
            Prim(group, "Wall_N", PrimitiveType.Cube, V(0, 1.6f, 3.15f), new Vector3(8.6f, 3.2f, 0.3f), concrete);
            Prim(group, "Wall_S", PrimitiveType.Cube, V(0, 1.6f, -3.15f), new Vector3(8.6f, 3.2f, 0.3f), concrete);
            Prim(group, "Wall_W", PrimitiveType.Cube, V(-4.15f, 1.6f, 0), new Vector3(0.3f, 3.2f, 6.6f), concrete);
            Prim(group, "Wall_E_South", PrimitiveType.Cube, V(4.15f, 1.6f, -1.9f), new Vector3(0.3f, 3.2f, 2.2f), concrete);
            Prim(group, "Wall_E_North", PrimitiveType.Cube, V(4.15f, 1.6f, 1.9f), new Vector3(0.3f, 3.2f, 2.2f), concrete);
            Prim(group, "Wall_E_Lintel", PrimitiveType.Cube, V(4.15f, 2.9f, 0), new Vector3(0.3f, 0.6f, 1.6f), concrete);
            Prim(group, "Roof", PrimitiveType.Cube, V(0, 3.3f, 0), new Vector3(8.6f, 0.2f, 6.6f), concrete);
            WorldText(group, "Sign", "MAINTENANCE 03", V(4.32f, 2.85f, 0), -90f, 2.2f, new Color(0.9f, 0.9f, 0.85f), 3f);

            var lampGo = new GameObject("CeilingLamp");
            lampGo.transform.SetParent(group, false);
            lampGo.transform.position = V(0, 2.9f, 0);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 9f;
            lamp.intensity = 2.5f;
            lamp.color = new Color(1f, 0.85f, 0.6f);

            // Lockers: Blue and Green on the north wall (facing south), Red and Yellow on the south wall (facing north)
            Vector2[] lockerOffset = { new Vector2(-2.5f, 2.75f), new Vector2(-1.5f, -2.75f), new Vector2(1.5f, 2.75f), new Vector2(2.5f, -2.75f) };
            var digits = new TMP_Text[4];
            var glows = new TMP_Text[4];
            var stencil = new Color(0.08f, 0.08f, 0.08f);
            for (int i = 0; i < 4; i++)
            {
                bool north = lockerOffset[i].y > 0f;
                float x = lockerOffset[i].x, z = lockerOffset[i].y;
                Prim(group, $"Locker_{LockerColorNames[i]}", PrimitiveType.Cube, V(x, 1f, z),
                    new Vector3(0.9f, 2f, 0.5f), Mat($"Locker_{LockerColorNames[i]}", LockerColors[i], Color.black));
                float faceZ = z + (north ? -0.26f : 0.26f);
                float yaw = north ? 0f : 180f;
                digits[i] = WorldText(group, $"Digit_{LockerColorNames[i]}", "0", V(x, 1.45f, faceZ), yaw, 6f, stencil, 0.8f);
                glows[i] = WorldText(group, $"GlowDigit_{LockerColorNames[i]}", "0", V(x, 0.7f, faceZ + (north ? -0.005f : 0.005f)),
                    yaw, 4.5f, new Color(0.5f, 1f, 0.45f), 0.8f);
            }

            // Wall message (west wall, facing the door) and decoys
            var message = WorldText(group, "WallMessage", "ARMORY CODE", V(-3.98f, 1.9f, 0), -90f, 3.5f, new Color(0.75f, 0.1f, 0.08f), 5.6f);
            message.enableAutoSizing = true;
            message.fontSizeMin = 1.2f;
            message.fontSizeMax = 3.5f;
            message.rectTransform.sizeDelta = new Vector2(5.6f, 1.6f);
            var decoyColor = new Color(0.85f, 0.85f, 0.8f);
            Prim(group, "SupplyShelf", PrimitiveType.Cube, V(-3.3f, 0.45f, -1.8f), new Vector3(1.1f, 0.9f, 1.2f),
                Mat("Crate_Wood", new Color(0.55f, 0.38f, 0.2f), Color.black));
            var decoys = new[]
            {
                WorldText(group, "Decoy_Bay", "BAY 0", V(0.5f, 2.55f, -2.98f), 180f, 2.5f, decoyColor, 2f),
                WorldText(group, "Decoy_Shelf", "SHELF 0", V(-2.73f, 0.5f, -1.8f), -90f, 1.6f, decoyColor, 1.1f),
                WorldText(group, "Decoy_Unit", "UNIT 00", V(-0.5f, 2.55f, 2.98f), 0f, 2.5f, decoyColor, 2f)
            };

            var puzzle = group.gameObject.AddComponent<ArmoryCodePuzzle>();
            var pso = new SerializedObject(puzzle);
            pso.FindProperty("wallMessage").objectReferenceValue = message;
            SetObjects(pso.FindProperty("lockerDigits"), digits);
            SetObjects(pso.FindProperty("glowDigits"), glows);
            SetObjects(pso.FindProperty("decoys"), decoys);
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Light switch inside, next to the doorway
            var plate = Prim(group, "LightSwitch", PrimitiveType.Cube, V(3.95f, 1.3f, 1.6f), new Vector3(0.1f, 0.3f, 0.2f),
                Mat("Prop_SwitchPlate", new Color(0.8f, 0.8f, 0.75f), Color.black));
            plate.layer = interactLayer;
            var leverPivot = new GameObject("LeverPivot").transform;
            leverPivot.SetParent(plate.transform, true);
            leverPivot.position = plate.transform.position + new Vector3(-0.06f, 0, 0);
            var leverHandle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leverHandle.name = "Lever";
            Object.DestroyImmediate(leverHandle.GetComponent<Collider>());
            leverHandle.transform.SetParent(leverPivot, true);
            leverHandle.transform.position = leverPivot.position + new Vector3(-0.03f, 0.05f, 0);
            leverHandle.transform.localScale = new Vector3(0.4f, 0.4f, 0.15f);
            leverHandle.GetComponent<Renderer>().sharedMaterial = Mat("Breaker_Handle", new Color(0.9f, 0.75f, 0.1f), new Color(0.4f, 0.3f, 0f));
            var lightSwitch = plate.AddComponent<LightSwitch>();
            var lso = new SerializedObject(lightSwitch);
            SetObjects(lso.FindProperty("lights"), lamp);
            lso.FindProperty("lever").objectReferenceValue = leverPivot;
            lso.FindProperty("puzzle").objectReferenceValue = puzzle;
            lso.ApplyModifiedPropertiesWithoutUndo();

            // Radio line + "visited" flag when the player steps inside
            var visit = new GameObject("Radio_CodeBunker");
            visit.transform.SetParent(group, false);
            visit.transform.position = V(0, 1.5f, 0);
            visit.AddComponent<BoxCollider>().size = new Vector3(7f, 3f, 5f);
            var radio = visit.AddComponent<RadioTrigger>();
            var rso = new SerializedObject(radio);
            rso.FindProperty("line").stringValue = "This is the place. The code's in there somewhere – check everything, and don't trust the first number you see.";
            rso.FindProperty("persistentId").stringValue = "CodeBunkerVisited";
            rso.ApplyModifiedPropertiesWithoutUndo();

            PaintBunkerPath(terrain, new Vector2(cx + 4.6f, cz));
        }

        /// <summary>
        /// Dirt path from the safe room door, curving northwest away from the road patrols, to the bunker door.
        /// Repaints the base roads first so paths from earlier builds don't linger.
        /// </summary>
        static void PaintBunkerPath(Terrain terrain, Vector2 bunkerDoor)
        {
            GreyboxBuilder.PaintRoads(terrain.terrainData);
            PaintDirtPath(terrain, new Vector2(93, 51), new Vector2(80, 60), new Vector2(66, 78), bunkerDoor);
            // Side path east from the road to the generator bunker's doorway
            PaintDirtPath(terrain, new Vector2(106, 72), new Vector2(130, 70), new Vector2(150, 72),
                new Vector2(GeneratorBunkerCenter.x - 4.6f, GeneratorBunkerCenter.y));
        }

        /// <summary>
        /// Repaints the bunker path to wherever the bunker stands now in the open island scene
        /// (after moving it by hand). Changes nothing else. Bridge command: bunker-path.
        /// </summary>
        [MenuItem("Tools/Signal Lost/Repaint Bunker Path")]
        public static void RepaintBunkerPathToCurrentBunker()
        {
            var puzzle = Object.FindFirstObjectByType<ArmoryCodePuzzle>();
            var terrain = Terrain.activeTerrain;
            if (puzzle == null || terrain == null)
            {
                Debug.LogWarning("[Signal Lost] Open IslandExterior first (bunker or terrain not found).");
                return;
            }
            // The doorway is in the east wall, 4.6 m from the floor center (rotated with the bunker).
            var floor = puzzle.transform.Find("Floor");
            Vector3 door = floor.position + puzzle.transform.rotation * new Vector3(4.6f, 0, 0);
            PaintBunkerPath(terrain, new Vector2(door.x, door.z));
            AssetDatabase.SaveAssets();
            Debug.Log($"[Signal Lost] Bunker path repainted to the door at {door}.");
        }

        // ================================================================== generator bunker (elevator power)

        /// <summary>Where the generator bunker stands (island X/Z), east of the road. The door faces west, toward the road.</summary>
        static readonly Vector2 GeneratorBunkerCenter = new Vector2(175f, 75f);

        /// <summary>
        /// Guarded bunker holding the main power lever. Turning it on powers the freight elevator.
        /// Guarded by 2 Enforcers and 2 Recon Stingers, with a red barrel near the door.
        /// </summary>
        static void BuildGeneratorBunker(Transform group, Terrain terrain)
        {
            float cx = GeneratorBunkerCenter.x, cz = GeneratorBunkerCenter.y;
            float gy = terrain.SampleHeight(new Vector3(cx, 0, cz));
            Vector3 V(float dx, float dy, float dz) => new Vector3(cx + dx, gy + dy, cz + dz);
            Vector3 At(float x, float z) => new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, z)), z);
            var concrete = Mat("Bunker_Concrete", new Color(0.55f, 0.55f, 0.52f), Color.black);

            // Shell: doorway in the west wall
            Prim(group, "Floor", PrimitiveType.Cube, V(0, -0.05f, 0), new Vector3(8.6f, 0.2f, 6.6f), concrete);
            Prim(group, "Wall_N", PrimitiveType.Cube, V(0, 1.6f, 3.15f), new Vector3(8.6f, 3.2f, 0.3f), concrete);
            Prim(group, "Wall_S", PrimitiveType.Cube, V(0, 1.6f, -3.15f), new Vector3(8.6f, 3.2f, 0.3f), concrete);
            Prim(group, "Wall_E", PrimitiveType.Cube, V(4.15f, 1.6f, 0), new Vector3(0.3f, 3.2f, 6.6f), concrete);
            Prim(group, "Wall_W_South", PrimitiveType.Cube, V(-4.15f, 1.6f, -1.9f), new Vector3(0.3f, 3.2f, 2.2f), concrete);
            Prim(group, "Wall_W_North", PrimitiveType.Cube, V(-4.15f, 1.6f, 1.9f), new Vector3(0.3f, 3.2f, 2.2f), concrete);
            Prim(group, "Wall_W_Lintel", PrimitiveType.Cube, V(-4.15f, 2.9f, 0), new Vector3(0.3f, 0.6f, 1.6f), concrete);
            Prim(group, "Roof", PrimitiveType.Cube, V(0, 3.3f, 0), new Vector3(8.6f, 0.2f, 6.6f), concrete);
            WorldText(group, "Sign", "GENERATOR 01", V(-4.32f, 2.85f, 0), 90f, 2.2f, new Color(0.95f, 0.8f, 0.2f), 3f);

            // Generator against the north wall
            Prim(group, "Generator", PrimitiveType.Cube, V(0, 0.8f, 2.1f), new Vector3(3f, 1.6f, 1.4f),
                Mat("Prop_Generator", new Color(0.2f, 0.32f, 0.22f), Color.black));
            Prim(group, "Generator_Exhaust", PrimitiveType.Cylinder, V(1f, 2.1f, 2.3f), new Vector3(0.3f, 0.5f, 0.3f),
                Mat("Prop_Pipe", new Color(0.5f, 0.3f, 0.15f), Color.black));

            // Lights: dim red emergency light always on; work light comes on with the power
            var emergency = new GameObject("EmergencyLight").AddComponent<Light>();
            emergency.transform.SetParent(group, false);
            emergency.transform.position = V(0, 2.9f, 0);
            emergency.type = LightType.Point;
            emergency.range = 8f;
            emergency.intensity = 0.8f;
            emergency.color = new Color(1f, 0.15f, 0.1f);
            var work = new GameObject("WorkLight").AddComponent<Light>();
            work.transform.SetParent(group, false);
            work.transform.position = V(0, 2.9f, -0.5f);
            work.type = LightType.Point;
            work.range = 10f;
            work.intensity = 3f;
            work.color = new Color(1f, 0.9f, 0.7f);
            work.enabled = false;

            // Main power lever on the east wall (knife-switch style, swings up when turned on)
            var panel = Prim(group, "MainPowerSwitch", PrimitiveType.Cube, V(3.92f, 1.3f, -1.2f), new Vector3(0.15f, 0.8f, 0.6f),
                Mat("Prop_PowerPanel", new Color(0.85f, 0.7f, 0.1f), new Color(0.3f, 0.2f, 0f)));
            panel.layer = interactLayer;
            WorldText(group, "PowerLabel", "MAIN POWER", V(3.83f, 1.9f, -1.2f), 90f, 1.8f, new Color(0.1f, 0.1f, 0.1f), 1.2f);
            var pivot = new GameObject("LeverPivot").transform;
            pivot.SetParent(panel.transform, true);
            pivot.position = panel.transform.position + new Vector3(-0.12f, 0, 0);
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            handle.transform.SetParent(pivot, true);
            handle.transform.position = pivot.position + new Vector3(-0.05f, 0.25f, 0);
            handle.transform.localScale = new Vector3(0.6f, 0.6f, 0.15f);
            handle.GetComponent<Renderer>().sharedMaterial = Mat("Prop_LeverHandle", new Color(0.15f, 0.15f, 0.15f), Color.black);

            var powerSwitch = panel.AddComponent<PowerSwitch>();
            var pso = new SerializedObject(powerSwitch);
            pso.FindProperty("lever").objectReferenceValue = pivot;
            SetObjects(pso.FindProperty("lightsToTurnOn"), work);
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Radio line when the power comes back
            var radioGo = new GameObject("Radio_PowerRestored");
            radioGo.transform.SetParent(group, false);
            var radio = radioGo.AddComponent<RadioTrigger>();
            var rso = new SerializedObject(radio);
            rso.FindProperty("line").stringValue = "Power's back – I can see the freight elevator lights from here! Head north up the road and ride it down.";
            rso.FindProperty("persistentId").stringValue = "Radio_PowerRestored";
            rso.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(powerSwitch.onPoweredOn, new UnityAction(radio.Play));

            // Supplies inside
            PlacePickup(group, medkit, V(-2.6f, 0.4f, -2.3f), "Generator_Medkit", null);
            PlacePickup(group, battery, V(-1.6f, 0.4f, -2.3f), "Generator_Battery", null);

            // Guards: 2 Enforcers + 2 Recon Stingers around the doorway, and a barrel for a smart opener
            PlaceEnemy(group, enforcer, At(cx - 10, cz + 5), "Gen_E1", At(cx - 12, cz + 7), At(cx - 9, cz - 5));
            PlaceEnemy(group, enforcer, At(cx - 3, cz + 9), "Gen_E2", At(cx - 5, cz + 10), At(cx + 5, cz + 8));
            PlaceEnemy(group, stinger, At(cx - 8, cz - 3), "Gen_S1", At(cx - 8, cz - 3), At(cx - 15, cz + 1));
            PlaceEnemy(group, stinger, At(cx - 1, cz - 7), "Gen_S2", At(cx - 1, cz - 7), At(cx + 7, cz - 5));
            Place(group, barrel, At(cx - 9.5f, cz + 1.5f));
        }

        static TMP_Text WorldText(Transform parent, string name, string text, Vector3 position, float yaw, float size, Color color, float width)
        {
            var tmp = new GameObject(name).AddComponent<TextMeshPro>();
            tmp.transform.SetParent(parent, false);
            tmp.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(width, size * 0.25f);
            return tmp;
        }

        /// <summary>Paints a 3 m dirt path on the island terrain (layer 1 = dirt) along the given points.</summary>
        static void PaintDirtPath(Terrain terrain, params Vector2[] points)
        {
            var data = terrain.terrainData;
            int res = data.alphamapResolution;
            var maps = data.GetAlphamaps(0, 0, res, res);
            for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                var p = new Vector2(xi / (float)(res - 1) * data.size.x, zi / (float)(res - 1) * data.size.z);
                float dist = float.MaxValue;
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Vector2 ab = points[i + 1] - points[i];
                    float t = Mathf.Clamp01(Vector2.Dot(p - points[i], ab) / ab.sqrMagnitude);
                    dist = Mathf.Min(dist, Vector2.Distance(p, points[i] + t * ab));
                }
                float dirt = 1f - Mathf.InverseLerp(1.2f, 2f, dist);
                float oldDirt = maps[zi, xi, 1];
                if (dirt <= oldDirt) continue;
                // Scale the other layers (grass, and rock/sand after the art pass) so the weights still add up to 1
                float keep = (1f - dirt) / (1f - oldDirt); // oldDirt < dirt <= 1, so no divide by zero
                for (int k = 0; k < maps.GetLength(2); k++)
                    if (k != 1) maps[zi, xi, k] *= keep;
                maps[zi, xi, 1] = dirt;
            }
            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);
        }

        // ================================================================== bunker

        static void PopulateBunker()
        {
            var scene = EditorSceneManager.OpenScene(Bunker);
            var root = ResetRoot();
            new GameObject("ObjectiveManager", typeof(ObjectiveManager)).transform.SetParent(root.transform);
            SetPortalRequirement("ElevatorPanel_Up", ScenePortal.Requirement.FusesOneAndTwo);
            var upPanel = GameObject.Find("ElevatorPanel_Up");
            if (upPanel != null)
            {
                var status = Prim(root.transform, "ElevatorStatusLight", PrimitiveType.Sphere, upPanel.transform.position + Vector3.up * 0.55f,
                    Vector3.one * 0.15f, Mat("Elevator_StatusLight", new Color(1f, 0.15f, 0.1f), new Color(3f, 0.4f, 0.3f)));
                Object.DestroyImmediate(status.GetComponent<Collider>());
                var upSo = new SerializedObject(upPanel.GetComponent<ScenePortal>());
                upSo.FindProperty("requiredClearedId").stringValue = WiringPuzzle.SolvedId;
                upSo.FindProperty("missingClearedMessage").stringValue = "Control circuit offline – rewire the junction box in the hub";
                upSo.FindProperty("statusLight").objectReferenceValue = status.GetComponent<Renderer>();
                upSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Hub: ammo locker, supplies, wing doors, a patrolling drone
            var hub = Group(root, "Hub");
            var locker = Prim(hub, "AmmoLocker", PrimitiveType.Cube, new Vector3(6.5f, 1f, 27.4f), new Vector3(2f, 2f, 0.8f),
                Mat("Prop_AmmoLocker", new Color(0.2f, 0.5f, 0.6f), new Color(0.1f, 0.6f, 0.8f)));
            locker.layer = interactLayer;
            locker.AddComponent<AmmoLocker>();
            PlacePickup(hub, medkit, new Vector3(-5f, 0.4f, 13.5f), "Bunker_Medkit1", null);
            PlacePickup(hub, battery, new Vector3(5f, 0.4f, 13.5f), "Bunker_Battery1", null);
            MakeDoor(hub, "Door_WingA", new Vector3(-8f, 0, 20f), new Vector3(0.2f, 3.2f, 4f), new Vector3(0, 3.3f, 0), alongZ: true);
            MakeDoor(hub, "Door_WingB", new Vector3(8f, 0, 20f), new Vector3(0.2f, 3.2f, 4f), new Vector3(0, 3.3f, 0), alongZ: true);
            PlaceEnemy(hub, stinger, new Vector3(0, 0, 24), "Bunker_Hub_S1", new Vector3(-5, 0, 24), new Vector3(5, 0, 16));

            BuildWingA(Group(root, "WingA"));
            BuildWingB(Group(root, "WingB"));
            BuildJunctionBox(Group(root, "JunctionBox"));

            EnsurePlayer(new Vector3(0, 0, -1));
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildWingA(Transform wing)
        {
            var alarm = new GameObject("AlarmSpawner").AddComponent<AlarmSpawner>();
            alarm.transform.SetParent(wing, false);
            SetRefs(alarm, "spawnPoints", Points(wing, "AlarmPoint", new Vector3(-40, 0, 24), new Vector3(-40, 0, 16), new Vector3(-29, 0, 20)));

            // Laser corridor: three emitters, three beams each
            var red = Mat("Laser_Red", new Color(1f, 0.1f, 0.05f), new Color(4f, 0.2f, 0.1f));
            foreach (float x in new[] { -13f, -19f, -25f })
            {
                var emitter = Prim(wing, $"LaserEmitter_{-x}", PrimitiveType.Cube, new Vector3(x, 1.1f, 21.7f),
                    new Vector3(0.35f, 1.6f, 0.3f), Mat("Laser_Emitter", new Color(0.2f, 0.2f, 0.22f), new Color(2f, 0.1f, 0.05f)));
                var beams = new List<GameObject>();
                foreach (float h in new[] { 0.45f, 1.05f, 1.65f })
                {
                    var beam = Prim(emitter.transform, "Beam", PrimitiveType.Cube, new Vector3(x, h, 20f), new Vector3(0.03f, 0.03f, 3.7f), red);
                    var box = beam.GetComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(3f, 3f, 1f); // ~9 cm thick
                    beam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    beams.Add(beam);
                }
                var trip = emitter.AddComponent<LaserTripwire>();
                var so = new SerializedObject(trip);
                SetObjects(so.FindProperty("beams"), beams.ToArray());
                so.FindProperty("emitterLight").objectReferenceValue = emitter.GetComponent<Renderer>();
                so.FindProperty("alarm").objectReferenceValue = alarm;
                so.FindProperty("alarmEnemyPrefab").objectReferenceValue = stinger;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Maintenance log terminal (before the lasers)
            var terminal = Prim(wing, "MaintenanceTerminal", PrimitiveType.Cube, new Vector3(-10f, 1.3f, 21.75f),
                new Vector3(0.7f, 0.5f, 0.1f), Mat("Prop_Screen", new Color(0.1f, 0.3f, 0.3f), new Color(0.2f, 1.2f, 1f)));
            terminal.layer = interactLayer;
            var log = terminal.AddComponent<DialogueSource>();
            var lso = new SerializedObject(log);
            lso.FindProperty("speaker").stringValue = "Maintenance Log";
            lso.FindProperty("prompt").stringValue = "Read Maintenance Log";
            lso.ApplyModifiedPropertiesWithoutUndo();

            // Breaker room: three levers, labels, glass pod around Fuse 1
            var puzzle = new GameObject("BreakerPuzzle").AddComponent<BreakerPuzzle>();
            puzzle.transform.SetParent(wing, false);
            var levers = new List<Lever>();
            string[] labels = { "COOLANT", "MAIN", "AUX" };
            for (int i = 0; i < 3; i++)
            {
                float x = -39f + i * 3f;
                var lever = MakeLever(wing, labels[i], new Vector3(x, 1.3f, 25.75f), puzzle);
                levers.Add(lever);
            }
            var glass = Prim(wing, "Fuse1_PodGlass", PrimitiveType.Cube, new Vector3(-40f, 1.95f, 20f), Vector3.one * 0.7f,
                Mat("Glass", new Color(0.6f, 0.9f, 1f, 0.3f), Color.black, transparent: true));
            PlacePickup(wing, fuse, new Vector3(-40f, 1.85f, 20f), "Pickup_Fuse1", null, fuseNumber: 1);

            var pso = new SerializedObject(puzzle);
            SetObjects(pso.FindProperty("levers"), levers.ToArray());
            pso.FindProperty("maintenanceLog").objectReferenceValue = log;
            pso.FindProperty("podGlass").objectReferenceValue = glass;
            pso.FindProperty("alarm").objectReferenceValue = alarm;
            pso.FindProperty("alarmEnemyPrefab").objectReferenceValue = enforcer;
            pso.ApplyModifiedPropertiesWithoutUndo();

            PlaceEnemy(wing, enforcer, new Vector3(-36, 0, 22), "Bunker_A_E1", new Vector3(-39, 0, 17), new Vector3(-33, 0, 23));

            BuildGaugeLock(wing);
        }

        /// <summary>
        /// Wing A gauge lock: three gauges spread between the laser sets show this run's readings; the breaker room
        /// door's keypad takes them in order (first to last, from the hub).
        /// </summary>
        static void BuildGaugeLock(Transform wing)
        {
            var dialMat = Mat("Gauge_Dial", new Color(0.92f, 0.92f, 0.88f), Color.black);
            var rimMat = Mat("Gauge_Rim", new Color(0.25f, 0.25f, 0.27f), Color.black);
            var needleMat = Mat("Gauge_Needle", new Color(0.85f, 0.1f, 0.05f), new Color(0.4f, 0.02f, 0f));
            float[] xs = { -11.5f, -16f, -22f };
            for (int i = 0; i < 3; i++)
            {
                var center = new Vector3(xs[i], 1.7f, 21.83f);
                var rim = Prim(wing, $"Gauge{i + 1}_Rim", PrimitiveType.Cylinder, center + new Vector3(0, 0, 0.01f), new Vector3(0.56f, 0.02f, 0.56f), rimMat);
                rim.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Object.DestroyImmediate(rim.GetComponent<Collider>());
                var dial = Prim(wing, $"Gauge{i + 1}", PrimitiveType.Cylinder, center, new Vector3(0.5f, 0.02f, 0.5f), dialMat);
                dial.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Object.DestroyImmediate(dial.GetComponent<Collider>());

                for (int v = 0; v <= 9; v++)
                {
                    var offset = Quaternion.Euler(0, 0, Gauge.AngleFor(v)) * Vector3.up * 0.19f;
                    WorldText(wing, $"Gauge{i + 1}_Mark{v}", v.ToString(), center + offset + new Vector3(0, 0, -0.035f), 0f, 0.7f,
                        new Color(0.1f, 0.1f, 0.1f), 0.1f);
                }
                var pivot = new GameObject($"Gauge{i + 1}_NeedlePivot").transform;
                pivot.SetParent(wing, false);
                pivot.position = center + new Vector3(0, 0, -0.04f);
                var needle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                needle.name = "Needle";
                Object.DestroyImmediate(needle.GetComponent<Collider>());
                needle.transform.SetParent(pivot, false);
                needle.transform.localPosition = new Vector3(0, 0.07f, 0);
                needle.transform.localScale = new Vector3(0.02f, 0.15f, 0.01f);
                needle.GetComponent<Renderer>().sharedMaterial = needleMat;

                var gauge = dial.AddComponent<Gauge>();
                var gso = new SerializedObject(gauge);
                gso.FindProperty("needlePivot").objectReferenceValue = pivot;
                gso.FindProperty("index").intValue = i;
                gso.ApplyModifiedPropertiesWithoutUndo();
                WorldText(wing, $"Gauge{i + 1}_Label", $"PRESSURE {i + 1}", center + new Vector3(0, -0.4f, -0.03f), 0f, 1.2f,
                    new Color(0.85f, 0.85f, 0.8f), 0.8f);
            }

            // Breaker room door (east doorway) with its own keypad on the corridor's north wall
            var door = MakeDoor(wing, "Door_BreakerRoom", new Vector3(-30f, 0, 20f), new Vector3(0.2f, 3.2f, 4f), new Vector3(0, 3.3f, 0),
                lockedReason: "Locked – enter the 3-digit code on the keypad", startsLocked: true,
                unlockedWhenCleared: CodeKeypad.BreakerRoomUnlockedId, alongZ: true);
            var keypad = Prim(wing, "BreakerRoomKeypad", PrimitiveType.Cube, new Vector3(-28.8f, 1.3f, 21.78f), new Vector3(0.22f, 0.3f, 0.12f),
                Mat("Door_Keypad", new Color(0.1f, 0.1f, 0.1f), new Color(0.1f, 1f, 0.3f)));
            keypad.layer = interactLayer;
            var code = keypad.AddComponent<CodeKeypad>();
            var cso = new SerializedObject(code);
            cso.FindProperty("door").objectReferenceValue = door;
            cso.FindProperty("source").enumValueIndex = (int)CodeKeypad.CodeSource.Gauges;
            cso.FindProperty("unlockId").stringValue = CodeKeypad.BreakerRoomUnlockedId;
            cso.FindProperty("title").stringValue = "BREAKER ROOM ACCESS";
            cso.FindProperty("prompt").stringValue = "Enter Breaker Room Code";
            cso.ApplyModifiedPropertiesWithoutUndo();
            WorldText(wing, "BreakerRoomCodePlate", "CODE = PRESSURE READINGS\nFIRST TO LAST", new Vector3(-28.8f, 1.85f, 21.82f), 0f, 1.2f,
                new Color(0.95f, 0.8f, 0.2f), 1.4f);
        }

        static void BuildWingB(Transform wing)
        {
            // Electrified water over the sunken generator floor (replaces the greybox water visual)
            var greyboxWater = GameObject.Find("ElectrifiedWater");
            if (greyboxWater != null && greyboxWater.transform.root.name == "Greybox") greyboxWater.SetActive(false);

            var waterGo = new GameObject("ElectrifiedWater");
            waterGo.transform.SetParent(wing, false);
            waterGo.transform.position = new Vector3(26f, -0.05f, 20f);
            var volume = waterGo.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = new Vector3(19.4f, 0.6f, 15.4f);
            var visual = Prim(waterGo.transform, "WaterSurface", PrimitiveType.Cube, new Vector3(26f, -0.1f, 20f), new Vector3(19.4f, 0.02f, 15.4f),
                Mat("Water_Electrified", new Color(0.1f, 0.35f, 0.5f, 0.7f), new Color(0.1f, 0.5f, 0.8f), transparent: true));
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var water = waterGo.AddComponent<ElectrifiedWater>();
            SetRef(water, "waterVisual", visual.transform);

            // Stepping-stone crates to the drain controls
            var crateMat = Mat("Crate_Metal", new Color(0.4f, 0.42f, 0.38f), Color.black);
            Vector2[] steps = { new Vector2(17.4f, 20f), new Vector2(19.6f, 21.6f), new Vector2(21.8f, 23.2f),
                                new Vector2(24.2f, 24.6f), new Vector2(26.6f, 25.8f), new Vector2(29f, 26.6f) };
            foreach (var s in steps)
                Prim(wing, "SteppingCrate", PrimitiveType.Cube, new Vector3(s.x, 0.15f, s.y), new Vector3(1.2f, 0.9f, 1.2f), crateMat).isStatic = true;

            // Quarantine room door: locked until the floodway is drained
            var qDoor = MakeDoor(wing, "Door_Quarantine", new Vector3(36f, 0, 20f), new Vector3(0.2f, 3.2f, 4f), new Vector3(0, 3.3f, 0),
                lockedReason: "Locked – route the floodway to the drain first", startsLocked: true, unlockedWhenCleared: "FloodwayDrained", alongZ: true);

            BuildPipeRouting(wing, water, qDoor);

            // Quarantine lockdown around Fuse 2 (Aegis Warden joins the squad in Phase 3)
            var lockdown = new GameObject("LockdownRoom").AddComponent<LockdownRoom>();
            lockdown.transform.SetParent(wing, false);
            var lso = new SerializedObject(lockdown);
            SetObjects(lso.FindProperty("doors"), qDoor);
            SetObjects(lso.FindProperty("spawnPoints"),
                Points(wing, "LockdownPoint", new Vector3(40, 0, 26), new Vector3(40, 0, 14), new Vector3(50, 0, 26)));
            SetObjects(lso.FindProperty("squad"), enforcer, enforcer, stinger);
            lso.ApplyModifiedPropertiesWithoutUndo();

            var fuse2 = PlacePickup(wing, fuse, new Vector3(48f, 1.45f, 20f), "Pickup_Fuse2", null, fuseNumber: 2);
            UnityEventTools.AddPersistentListener(fuse2.GetComponent<Pickup>().onPickedUp, new UnityAction(lockdown.Begin));
            PlacePickup(wing, medkit, new Vector3(51f, 0.4f, 13f), "Bunker_Medkit2", null);
        }

        /// <summary>
        /// Wing B drain controls on the north wall: a painted pipe schematic, three junction levers (A, B, C) and
        /// a PUMP handle. The schematic is drawn so that A=DOWN, B=UP, C=DOWN sends the water to the drain.
        /// </summary>
        static void BuildPipeRouting(Transform wing, ElectrifiedWater water, Door quarantineDoor)
        {
            var group = Group(wing, "DrainControls");
            const float wallZ = 27.83f, baseX = 23.4f, baseY = 1.2f;
            Vector3 W(float u, float v) => new Vector3(baseX + u, baseY + v, wallZ);
            var pipeMat = Mat("Schematic_Pipe", new Color(0.75f, 0.75f, 0.7f), new Color(0.15f, 0.15f, 0.14f));
            var drainMat = Mat("Schematic_Drain", new Color(0.3f, 0.8f, 1f), new Color(0.2f, 0.7f, 1f));
            var returnMat = Mat("Schematic_Return", new Color(0.9f, 0.25f, 0.15f), new Color(0.6f, 0.1f, 0.05f));

            Prim(group, "SchematicBoard", PrimitiveType.Cube, W(2.2f, 0.5f) + new Vector3(0, 0, 0.03f), new Vector3(5.6f, 2.6f, 0.02f),
                Mat("Schematic_Board", new Color(0.12f, 0.14f, 0.16f), Color.black)).GetComponent<Collider>().enabled = false;

            Renderer Pipe(float u1, float v1, float u2, float v2, Material mat)
            {
                var a = W(u1, v1); var b = W(u2, v2);
                var size = new Vector3(Mathf.Abs(b.x - a.x) + 0.07f, Mathf.Abs(b.y - a.y) + 0.07f, 0.02f);
                var pipe = Prim(group, "Pipe", PrimitiveType.Cube, (a + b) / 2f, size, mat);
                pipe.GetComponent<Collider>().enabled = false;
                return pipe.GetComponent<Renderer>();
            }
            void Label(string text, float u, float v, Color color) =>
                WorldText(group, $"Label_{text}", text, W(u, v) + new Vector3(0, 0, -0.02f), 0f, 1.3f, color, 1.2f);

            var white = new Color(0.9f, 0.9f, 0.85f);
            var red = new Color(1f, 0.4f, 0.3f);
            var gold = new Color(1f, 0.85f, 0.2f);
            void Small(string text, float u, float v, Color color) =>
                WorldText(group, $"Small_{text}_{u}_{v}", text, W(u, v) + new Vector3(0, 0, -0.02f), 0f, 0.9f, color, 0.6f);

            // Inlet → junction A
            var inlet = new[] { Pipe(-0.4f, 0.8f, 0.8f, 0.8f, pipeMat) }; Label("INLET", -0.6f, 1.05f, white);
            // A: UP → return, DOWN → junction B
            var aUp = new[] { Pipe(0.8f, 0.8f, 0.8f, 1.4f, pipeMat), Pipe(0.8f, 1.4f, 1.9f, 1.4f, returnMat) };
            Label("RETURN", 2.45f, 1.4f, red);
            var aDown = new[] { Pipe(0.8f, 0.8f, 0.8f, 0.2f, pipeMat), Pipe(0.8f, 0.2f, 1.8f, 0.2f, pipeMat) };
            // B: UP → junction C, DOWN → return
            var bUp = new[] { Pipe(1.8f, 0.2f, 1.8f, 0.8f, pipeMat), Pipe(1.8f, 0.8f, 2.8f, 0.8f, pipeMat) };
            var bDown = new[] { Pipe(1.8f, 0.2f, 1.8f, -0.4f, pipeMat), Pipe(1.8f, -0.4f, 2.9f, -0.4f, returnMat) };
            Label("RETURN", 3.45f, -0.4f, red);
            // C: UP → return, DOWN → drain
            var cUp = new[] { Pipe(2.8f, 0.8f, 2.8f, 1.4f, pipeMat), Pipe(2.8f, 1.4f, 3.9f, 1.4f, returnMat) };
            Label("RETURN", 4.45f, 1.4f, red);
            var cDown = new[] { Pipe(2.8f, 0.8f, 2.8f, 0.2f, pipeMat), Pipe(2.8f, 0.2f, 3.9f, 0.2f, drainMat) };
            Label("DRAIN", 4.4f, 0.2f, new Color(0.4f, 0.9f, 1f));

            // Junction letters, and UP / DOWN next to each junction's two branches
            Label("A", 0.55f, 0.8f, gold); Small("UP", 1.0f, 1.12f, gold); Small("DOWN", 1.05f, 0.48f, gold);
            Label("B", 1.55f, 0.2f, gold); Small("UP", 2.0f, 0.52f, gold); Small("DOWN", 2.05f, -0.12f, gold);
            Label("C", 2.55f, 0.8f, gold); Small("UP", 3.0f, 1.12f, gold); Small("DOWN", 3.05f, 0.48f, gold);
            WorldText(group, "Schematic_Title",
                "FLOODWAY ROUTING\n<size=65%>The lit pipe shows where the water will go. Flip levers A, B, C to send it to the DRAIN, then pull PUMP.</size>",
                W(2.2f, 1.85f) + new Vector3(0, 0, -0.02f), 0f, 1.3f, white, 5.4f);

            // Levers under the schematic, reachable from the last stepping crates
            var routing = group.gameObject.AddComponent<PipeRouting>();
            var levers = new PipeLever[3];
            string[] names = { "A", "B", "C" };
            for (int i = 0; i < 3; i++)
            {
                var basePos = new Vector3(25.6f + i * 1.3f, 0.75f, 27.78f);
                var box = Prim(group, $"JunctionLever_{names[i]}", PrimitiveType.Cube, basePos, new Vector3(0.35f, 0.5f, 0.15f),
                    Mat("Breaker_Box", new Color(0.3f, 0.32f, 0.3f), Color.black));
                box.layer = interactLayer;
                var handlePivot = new GameObject("HandlePivot").transform;
                handlePivot.SetParent(box.transform, true);
                handlePivot.position = basePos + new Vector3(0, 0, -0.09f);
                handlePivot.localScale = Vector3.one;
                var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = "Handle";
                Object.DestroyImmediate(handle.GetComponent<Collider>());
                handle.transform.SetParent(handlePivot, false);
                handle.transform.position = handlePivot.position + Vector3.up * 0.15f;
                handle.transform.localScale = new Vector3(0.06f / 0.35f, 0.3f / 0.5f, 0.06f / 0.15f);
                handle.GetComponent<Renderer>().sharedMaterial = Mat("Breaker_Handle", new Color(0.9f, 0.75f, 0.1f), new Color(0.4f, 0.3f, 0f));
                var stateLabel = WorldText(group, $"LeverLabel_{names[i]}", $"{names[i]}: UP", basePos + new Vector3(0, 0.42f, -0.1f), 0f, 1.5f,
                    new Color(1f, 0.85f, 0.2f), 1.1f);
                levers[i] = box.AddComponent<PipeLever>();
                var lso = new SerializedObject(levers[i]);
                lso.FindProperty("label").stringValue = names[i];
                lso.FindProperty("stateLabel").objectReferenceValue = stateLabel;
                lso.FindProperty("handle").objectReferenceValue = handlePivot;
                lso.FindProperty("routing").objectReferenceValue = routing;
                lso.ApplyModifiedPropertiesWithoutUndo();
            }

            var pump = Prim(group, "PumpHandle", PrimitiveType.Cube, new Vector3(29.6f, 0.85f, 27.75f), new Vector3(0.3f, 0.7f, 0.2f),
                Mat("Prop_ValveWheel", new Color(0.9f, 0.1f, 0.1f), new Color(0.6f, 0.05f, 0f)));
            pump.layer = interactLayer;
            SetRef(pump.AddComponent<PumpHandle>(), "routing", routing);
            WorldText(group, "PumpLabel", "PUMP", new Vector3(29.6f, 1.4f, 27.68f), 0f, 1.8f, new Color(1f, 0.4f, 0.3f), 0.8f);

            var surge = new GameObject("SurgePoint").transform;
            surge.SetParent(group, false);
            surge.position = new Vector3(24f, 0.2f, 22f);

            var rso = new SerializedObject(routing);
            SetObjects(rso.FindProperty("levers"), levers);
            rso.FindProperty("water").objectReferenceValue = water;
            rso.FindProperty("doorToUnlock").objectReferenceValue = quarantineDoor;
            rso.FindProperty("surgePoint").objectReferenceValue = surge;
            SetObjects(rso.FindProperty("inletPipes"), inlet);
            SetObjects(rso.FindProperty("aUpPipes"), aUp);
            SetObjects(rso.FindProperty("aDownPipes"), aDown);
            SetObjects(rso.FindProperty("bUpPipes"), bUp);
            SetObjects(rso.FindProperty("bDownPipes"), bDown);
            SetObjects(rso.FindProperty("cUpPipes"), cUp);
            SetObjects(rso.FindProperty("cDownPipes"), cDown);
            rso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Hub junction box on the south wall, right of the elevator corridor: a 3×3 wire tile grid that restores the
        /// elevator's control circuit. Tiles are laid out as the player sees them (row 0 at the top, column 0 on the left).
        /// </summary>
        static void BuildJunctionBox(Transform group)
        {
            const float spacing = 0.65f;
            var root = new GameObject("WireGrid").transform;
            root.SetParent(group, false);
            root.position = new Vector3(5.6f, 2.05f, 12.2f);

            Prim(group, "JunctionBoxBacking", PrimitiveType.Cube, new Vector3(4.95f, 1.4f, 12.17f), new Vector3(3.2f, 2.3f, 0.02f),
                Mat("Junction_Backing", new Color(0.1f, 0.11f, 0.12f), Color.black)).GetComponent<Collider>().enabled = false;
            WorldText(group, "JunctionTitle", "ELEVATOR CONTROL – JUNCTION 4\n<size=70%>connect IN to OUT</size>",
                new Vector3(4.95f, 2.75f, 12.24f), 180f, 1.4f, new Color(0.4f, 0.9f, 1f), 3f);

            var plateMat = Mat("Junction_Tile", new Color(0.18f, 0.19f, 0.2f), Color.black);
            var barMat = Mat("Junction_Wire", new Color(0.2f, 0.2f, 0.22f), Color.black);
            var tiles = new WireTile[WiringPuzzle.Size * WiringPuzzle.Size];
            for (int row = 0; row < WiringPuzzle.Size; row++)
            for (int col = 0; col < WiringPuzzle.Size; col++)
            {
                var tileGo = new GameObject($"Tile_{row}_{col}");
                tileGo.layer = interactLayer;
                tileGo.transform.SetParent(root, false);
                tileGo.transform.localPosition = new Vector3(-col * spacing, -row * spacing, 0f);
                var collider = tileGo.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.6f, 0.6f, 0.08f);

                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "Plate";
                Object.DestroyImmediate(plate.GetComponent<Collider>());
                plate.transform.SetParent(tileGo.transform, false);
                plate.transform.localScale = new Vector3(0.6f, 0.6f, 0.04f);
                plate.GetComponent<Renderer>().sharedMaterial = plateMat;

                var pivot = new GameObject("Pivot").transform;
                pivot.SetParent(tileGo.transform, false);
                pivot.localPosition = new Vector3(0, 0, 0.03f);
                // Bars at rotation 0, in screen directions: North (+y), East (screen right = -x), South, West
                Vector3[] positions = { new Vector3(0, 0.15f, 0), new Vector3(-0.15f, 0, 0), new Vector3(0, -0.15f, 0), new Vector3(0.15f, 0, 0) };
                Vector3[] sizes = { new Vector3(0.08f, 0.38f, 0.03f), new Vector3(0.38f, 0.08f, 0.03f), new Vector3(0.08f, 0.38f, 0.03f), new Vector3(0.38f, 0.08f, 0.03f) };
                var bars = new Renderer[4];
                for (int b = 0; b < 4; b++)
                {
                    var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.name = $"Bar{b}";
                    Object.DestroyImmediate(bar.GetComponent<Collider>());
                    bar.transform.SetParent(pivot, false);
                    bar.transform.localPosition = positions[b];
                    bar.transform.localScale = sizes[b];
                    bar.GetComponent<Renderer>().sharedMaterial = barMat;
                    bars[b] = bar.GetComponent<Renderer>();
                }

                var tile = tileGo.AddComponent<WireTile>();
                var tso = new SerializedObject(tile);
                tso.FindProperty("pivot").objectReferenceValue = pivot;
                SetObjects(tso.FindProperty("bars"), bars);
                tso.ApplyModifiedPropertiesWithoutUndo();
                tiles[row * WiringPuzzle.Size + col] = tile;
            }

            var inMarker = WorldText(group, "InputMarker", "IN >", Vector3.zero, 180f, 1.6f, new Color(0.4f, 1f, 0.5f), 0.6f).transform;
            inMarker.SetParent(root, false);
            var outMarker = WorldText(group, "OutputMarker", "> OUT", Vector3.zero, 180f, 1.6f, new Color(0.4f, 1f, 0.5f), 0.7f).transform;
            outMarker.SetParent(root, false);
            inMarker.localRotation = outMarker.localRotation = Quaternion.Euler(0, 180f, 0);

            var puzzle = root.gameObject.AddComponent<WiringPuzzle>();
            var pso = new SerializedObject(puzzle);
            SetObjects(pso.FindProperty("tiles"), tiles);
            pso.FindProperty("inputMarker").objectReferenceValue = inMarker;
            pso.FindProperty("outputMarker").objectReferenceValue = outMarker;
            pso.FindProperty("tileSpacing").floatValue = spacing;
            pso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== sandbox

        static void PopulateSandbox()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Sandbox) == null) PlayerPrefabBuilder.BuildSandbox();
            var scene = EditorSceneManager.OpenScene(Sandbox);
            var root = ResetRoot();

            if (Object.FindFirstObjectByType<SignalLost.DebugTools.DebugGrantPistol>() == null)
                new GameObject("DebugGrantPistol").AddComponent<SignalLost.DebugTools.DebugGrantPistol>().transform.SetParent(root.transform);
            Place(root.transform, enforcer, new Vector3(-8, 0, 14));
            Place(root.transform, stinger, new Vector3(8, 0, 14));
            Place(root.transform, barrel, new Vector3(-6, 0, 8));
            Place(root.transform, barrel, new Vector3(-5, 0, 9));
            Place(root.transform, crate, new Vector3(4, 0, 0));
            Place(root.transform, crate, new Vector3(5.5f, 0, 0));
            PlacePickup(root.transform, medkit, new Vector3(-3, 0.4f, -4), null, null);
            PlacePickup(root.transform, battery, new Vector3(3, 0.4f, -4), null, null);

            // The sandbox floor needs a NavMesh for the enemies
            var floor = GameObject.Find("Floor");
            if (!floor.TryGetComponent<NavMeshSurface>(out var surface)) surface = floor.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << enemyLayer));
            surface.BuildNavMesh();
            var path = "Assets/_Project/Art/Greybox/NavMesh_Sandbox_A.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);

            EnsurePlayer(new Vector3(0, 0, -6));
            EditorSceneManager.SaveScene(scene);
        }

        // ================================================================== placement helpers

        /// <summary>The Player prefab was just rebuilt; make sure each scene has one at its start position.</summary>
        internal static void EnsurePlayer(Vector3 position)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
                player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.PlayerPath));
            player.transform.position = position;
        }

        static GameObject ResetRoot()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            return new GameObject(RootName);
        }

        internal static Transform Group(GameObject root, string name) => Group(root.transform, name);

        internal static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        internal static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        internal static GameObject Child(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = root.layer;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static GameObject Place(Transform parent, GameObject prefab, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = position;
            return go;
        }

        internal static GameObject PlacePickup(Transform parent, GameObject prefab, Vector3 position, string id, string objective, int fuseNumber = 1)
        {
            var go = Place(parent, prefab, position);
            var so = new SerializedObject(go.GetComponent<Pickup>());
            so.FindProperty("persistentId").stringValue = id ?? "";
            so.FindProperty("objectiveOnPickup").stringValue = objective ?? "";
            so.FindProperty("fuseNumber").intValue = fuseNumber;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        internal static void PlaceEnemy(Transform parent, GameObject prefab, Vector3 position, string id, params Vector3[] waypointPositions)
        {
            var go = Place(parent, prefab, position);
            go.name = $"{prefab.name}_{id}";
            var holder = Group(parent, $"Waypoints_{id}");
            var points = new Transform[waypointPositions.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new GameObject($"WP{i}").transform;
                points[i].SetParent(holder, false);
                points[i].position = waypointPositions[i];
            }
            var so = new SerializedObject(go.GetComponent<EnemyAI>());
            so.FindProperty("persistentId").stringValue = id;
            SetObjects(so.FindProperty("waypoints"), points);
            so.ApplyModifiedPropertiesWithoutUndo();
            if (points.Length > 1) go.transform.rotation = Quaternion.LookRotation(Flat(points[1].position - points[0].position));
        }

        internal static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.001f ? Vector3.forward : v;
        }

        internal static Door MakeDoor(Transform parent, string name, Vector3 floorCenter, Vector3 panelSize, Vector3 openOffset,
            string lockedReason = "Locked", bool startsLocked = false, string unlockedWhenCleared = "", bool alongZ = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = floorCenter;
            root.layer = interactLayer;

            var panel = Prim(root.transform, "Panel", PrimitiveType.Cube, floorCenter + Vector3.up * panelSize.y / 2f, panelSize,
                Mat("Door_Panel", new Color(0.5f, 0.52f, 0.55f), Color.black));
            panel.layer = interactLayer;
            var obstacle = panel.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;

            // Keypads on both sides of the wall, next to the doorway
            float halfWidth = alongZ ? panelSize.z / 2f : panelSize.x / 2f;
            var keypadMat = Mat("Door_Keypad", new Color(0.1f, 0.1f, 0.1f), new Color(0.1f, 1f, 0.3f));
            foreach (float side in new[] { -0.2f, 0.2f })
            {
                Vector3 offset = alongZ ? new Vector3(side, 1.3f, halfWidth + 0.4f) : new Vector3(halfWidth + 0.4f, 1.3f, side);
                var keypad = Prim(root.transform, "Keypad", PrimitiveType.Cube, floorCenter + offset, new Vector3(0.12f, 0.22f, 0.15f), keypadMat);
                keypad.layer = interactLayer;
            }

            var door = root.AddComponent<Door>();
            var so = new SerializedObject(door);
            so.FindProperty("panel").objectReferenceValue = panel.transform;
            so.FindProperty("openOffset").vector3Value = openOffset;
            so.FindProperty("startsLocked").boolValue = startsLocked;
            so.FindProperty("lockedReason").stringValue = lockedReason;
            so.FindProperty("unlockedWhenCleared").stringValue = unlockedWhenCleared;
            so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        static Lever MakeLever(Transform parent, string label, Vector3 position, BreakerPuzzle puzzle)
        {
            var box = Prim(parent, $"Breaker_{label}", PrimitiveType.Cube, position, new Vector3(0.45f, 0.65f, 0.15f),
                Mat("Breaker_Box", new Color(0.3f, 0.32f, 0.3f), Color.black));
            box.layer = interactLayer;

            var pivot = new GameObject("HandlePivot").transform;
            pivot.SetParent(box.transform, true);
            pivot.position = position + new Vector3(0, 0, -0.1f);
            pivot.localScale = Vector3.one;
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            handle.transform.SetParent(pivot, false);
            handle.transform.position = pivot.position + Vector3.up * 0.18f;
            handle.transform.localScale = new Vector3(0.06f / 0.45f, 0.35f / 0.65f, 0.06f / 0.15f);
            handle.GetComponent<Renderer>().sharedMaterial = Mat("Breaker_Handle", new Color(0.9f, 0.75f, 0.1f), new Color(0.4f, 0.3f, 0f));

            var text = new GameObject($"Label_{label}").AddComponent<TextMeshPro>();
            text.transform.SetParent(parent, false);
            text.transform.position = position + new Vector3(0, 0.6f, -0.1f);
            text.transform.rotation = Quaternion.identity; // readable from inside the room (looking toward the wall)
            text.text = label;
            text.fontSize = 3f;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(2.5f, 0.6f);
            text.color = new Color(1f, 0.85f, 0.2f);

            var lever = box.AddComponent<Lever>();
            var so = new SerializedObject(lever);
            so.FindProperty("label").stringValue = label;
            so.FindProperty("handle").objectReferenceValue = pivot;
            so.FindProperty("puzzle").objectReferenceValue = puzzle;
            so.ApplyModifiedPropertiesWithoutUndo();
            return lever;
        }

        internal static Transform[] Points(Transform parent, string name, params Vector3[] positions)
        {
            var points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                points[i] = new GameObject($"{name}_{i}").transform;
                points[i].SetParent(parent, false);
                points[i].position = positions[i];
            }
            return points;
        }

        static void SetPortalRequirement(string objectName, ScenePortal.Requirement requirement)
        {
            var go = GameObject.Find(objectName);
            if (go == null) return;
            var so = new SerializedObject(go.GetComponent<ScenePortal>());
            so.FindProperty("requirement").enumValueIndex = (int)requirement;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
        }

        internal static void SetRef(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefs(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            SetObjects(so.FindProperty(property), values);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetObjects(SerializedProperty array, params Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        internal static void SetStrings(SerializedProperty array, params string[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        // ================================================================== assets

        internal static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        internal static Material Mat(string name, Color color, Color emission, bool transparent = false)
        {
            EnsureFolder(MatFolder);
            var path = $"{MatFolder}/{name}.mat";
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
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetFloat("_ZWrite", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
