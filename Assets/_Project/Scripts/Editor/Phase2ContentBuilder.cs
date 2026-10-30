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
        static int enemyLayer, interactLayer, notEnemy;

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

            PopulateIsland();
            PopulateBunker();
            PopulateSandbox();
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

        static Projectile SaveBolt(string name, Color color, float speed, float lifetime)
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

        static void SetupAI(EnemyAI ai, EnemyStats stats, Transform eye, Renderer sensor)
        {
            var so = new SerializedObject(ai);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.FindProperty("eye").objectReferenceValue = eye;
            so.FindProperty("sensorRenderer").objectReferenceValue = sensor;
            so.FindProperty("sightMask").intValue = notEnemy;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetHealth(Health health, float max)
        {
            var so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = max;
            so.FindProperty("destroyOnDeath").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static EnemyStats Stats(string name, System.Action<EnemyStats> configure)
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
                lockedReason: "Locked – talk to Dr. Reyes first", startsLocked: true, unlockedWhenCleared: "Briefing", alongZ: true);

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
                "My leg's broken, so it has to be you. I've unlocked the armory – take the Arc Pistol. Aim for their glowing eyes and visors.");
            SetStrings(dso.FindProperty("repeatLines"), "The fuses are in Wings A and B below, and one at the radio tower. Go – I'll guide you over the radio.");
            dso.FindProperty("persistentId").stringValue = "Briefing";
            dso.FindProperty("objectiveOnFinish").stringValue = "Take the Arc Pistol from the armory";
            dso.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(dialogue.onFinished, new UnityAction(door.Unlock));

            Prim(safe, "ArmoryTable", PrimitiveType.Cube, At(83.5f, 45, 0.45f), new Vector3(0.6f, 0.9f, 1.2f),
                Mat("Prop_Table", new Color(0.4f, 0.4f, 0.42f), Color.black));
            var pistol = PlacePickup(safe, pistolPickup, At(83.5f, 45, 1.05f), "Pickup_ArcPistol", "Ride the freight elevator down");

            // Elevator now needs the Arc Pistol
            SetPortalRequirement("ElevatorPanel_Down", ScenePortal.Requirement.ArcPistol);

            // Island road: a lone Enforcer to learn on, then a squad by a barrel
            var road = Group(root, "Road");
            PlaceEnemy(road, enforcer, At(100, 62), "Island_E1", At(96, 57), At(106, 70));
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

        // ================================================================== bunker

        static void PopulateBunker()
        {
            var scene = EditorSceneManager.OpenScene(Bunker);
            var root = ResetRoot();
            new GameObject("ObjectiveManager", typeof(ObjectiveManager)).transform.SetParent(root.transform);
            SetPortalRequirement("ElevatorPanel_Up", ScenePortal.Requirement.FusesOneAndTwo);

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

            // Stepping-stone crates to the valve
            var crateMat = Mat("Crate_Metal", new Color(0.4f, 0.42f, 0.38f), Color.black);
            Vector2[] steps = { new Vector2(17.4f, 20f), new Vector2(19.6f, 21.6f), new Vector2(21.8f, 23.2f),
                                new Vector2(24.2f, 24.6f), new Vector2(26.6f, 25.8f), new Vector2(29f, 26.6f) };
            foreach (var s in steps)
                Prim(wing, "SteppingCrate", PrimitiveType.Cube, new Vector3(s.x, 0.15f, s.y), new Vector3(1.2f, 0.9f, 1.2f), crateMat).isStatic = true;

            // Quarantine room door: locked until the floodway is drained
            var qDoor = MakeDoor(wing, "Door_Quarantine", new Vector3(36f, 0, 20f), new Vector3(0.2f, 3.2f, 4f), new Vector3(0, 3.3f, 0),
                lockedReason: "Locked – drain the floodway first", startsLocked: true, unlockedWhenCleared: "FloodwayDrained", alongZ: true);

            // Valve on the north wall above the last crate
            var pipe = Prim(wing, "ValvePipe", PrimitiveType.Cube, new Vector3(30f, 1.4f, 27.8f), Vector3.one * 0.3f,
                Mat("Prop_Pipe", new Color(0.5f, 0.3f, 0.15f), Color.black));
            pipe.GetComponent<Collider>().enabled = false;
            var wheel = Prim(wing, "ValveWheel", PrimitiveType.Cylinder, new Vector3(30f, 1.4f, 27.6f), new Vector3(0.6f, 0.04f, 0.6f),
                Mat("Prop_ValveWheel", new Color(0.9f, 0.1f, 0.1f), new Color(0.6f, 0.05f, 0f)));
            wheel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            wheel.layer = interactLayer;
            var valve = wheel.AddComponent<Valve>();
            var vso = new SerializedObject(valve);
            vso.FindProperty("wheel").objectReferenceValue = wheel.transform;
            vso.FindProperty("water").objectReferenceValue = water;
            vso.FindProperty("doorToUnlock").objectReferenceValue = qDoor;
            vso.ApplyModifiedPropertiesWithoutUndo();

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
        static void EnsurePlayer(Vector3 position)
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

        static Transform Group(GameObject root, string name) => Group(root.transform, name);

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject Child(GameObject root, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, bool keepCollider = true)
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

        static GameObject Place(Transform parent, GameObject prefab, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = position;
            return go;
        }

        static GameObject PlacePickup(Transform parent, GameObject prefab, Vector3 position, string id, string objective, int fuseNumber = 1)
        {
            var go = Place(parent, prefab, position);
            var so = new SerializedObject(go.GetComponent<Pickup>());
            so.FindProperty("persistentId").stringValue = id ?? "";
            so.FindProperty("objectiveOnPickup").stringValue = objective ?? "";
            so.FindProperty("fuseNumber").intValue = fuseNumber;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static void PlaceEnemy(Transform parent, GameObject prefab, Vector3 position, string id, params Vector3[] waypointPositions)
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

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.001f ? Vector3.forward : v;
        }

        static Door MakeDoor(Transform parent, string name, Vector3 floorCenter, Vector3 panelSize, Vector3 openOffset,
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
            text.transform.rotation = Quaternion.Euler(0, 180, 0);
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

        static Transform[] Points(Transform parent, string name, params Vector3[] positions)
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

        static void SetRef(Object target, string property, Object value)
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

        static void SetObjects(SerializedProperty array, params Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetStrings(SerializedProperty array, params string[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        // ================================================================== assets

        static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Material Mat(string name, Color color, Color emission, bool transparent = false)
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

        static void EnsureFolder(string path)
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
