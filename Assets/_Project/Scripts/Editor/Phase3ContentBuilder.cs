using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static SignalLost.EditorTools.Phase2ContentBuilder;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 3: Aegis Warden, Titan (Phase 1), menus prefab, Main Menu scene, and the full game loop placed
    /// under a root object named "Gameplay_Phase3": radio lines, checkpoints, Fuse 3, the tower console and
    /// wave defense, the service gate, the boss, and the extraction boat. Re-running rebuilds that root only.
    /// Run after the greybox and Phase 2 content.
    /// </summary>
    public static class Phase3ContentBuilder
    {
        const string RootName = "Gameplay_Phase3";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string MenusPath = "Assets/_Project/UI/GameMenus.prefab";
        const string Island = "Assets/_Project/Scenes/IslandExterior.unity";
        const string Bunker = "Assets/_Project/Scenes/UndergroundFacility.unity";
        const string MainMenu = "Assets/_Project/Scenes/MainMenu.unity";
        static readonly Vector2 Hill = new Vector2(130, 190);

        static GameObject enforcer, stinger, aegis, titan, medkit, fuse, menus;

        [MenuItem("Tools/Signal Lost/Build Phase 3 Content")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            enemyLayer = LayerMask.NameToLayer("Enemy");
            interactLayer = LayerMask.NameToLayer("Interactable");
            notEnemy = ~(1 << enemyLayer);

            HUDPrefabBuilder.Build();
            PlayerPrefabBuilder.BuildPlayer();
            enforcer = Load("Enemies/Enforcer");
            stinger = Load("Enemies/ReconStinger");
            medkit = Load("Pickups/Medkit");
            fuse = Load("Pickups/OverrideFuse");
            aegis = SaveAegis();
            titan = SaveTitan();
            EnemyArtBuilder.DressEnemies(); // keep the Engie and mech models on the rebuilt Aegis Warden and Titan
            aegis = Load("Enemies/AegisWarden");
            titan = Load("Enemies/Unit00_Titan");
            menus = BuildMenusPrefab();

            BuildMainMenuScene();
            PopulateIsland();
            PopulateBunker();
            UIArtBuilder.Build(); // restyle the rebuilt menus and Main Menu scene
            EditorSceneManager.OpenScene(Island);
            Debug.Log("[Signal Lost] Phase 3 content built.");
        }

        static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{path}.prefab");

        // ================================================================== enemies

        static GameObject SaveAegis()
        {
            var plasma = SaveBolt("HeavyPlasma", new Color(1f, 0.5f, 0.1f), 15f, 4f);
            var stats = Stats("Stats_AegisWarden", s =>
            {
                s.patrolSpeed = 1.5f; s.chaseSpeed = 2.5f; s.attackRange = 12f;
                s.sideStepWhenHit = false; s.advanceWhileAttacking = true; s.preferredAttackDistance = 8f;
            });

            var root = new GameObject("AegisWarden");
            root.layer = enemyLayer;
            var armor = Mat("Enemy_DarkArmor", new Color(0.15f, 0.16f, 0.18f), Color.black);
            var sensor = Mat("Enemy_Sensor", new Color(0.2f, 0.5f, 1f), new Color(0.8f, 2f, 4f));

            Child(root, "Body", PrimitiveType.Capsule, new Vector3(0, 1.17f, 0), new Vector3(0.95f, 1.17f, 0.95f), armor);
            Child(root, "Head", PrimitiveType.Cube, new Vector3(0, 2.1f, 0), new Vector3(0.45f, 0.4f, 0.45f), armor, keepCollider: false);
            var visor = Child(root, "Visor_WeakPoint", PrimitiveType.Cube, new Vector3(0, 2.14f, 0.22f), new Vector3(0.4f, 0.12f, 0.1f), sensor);
            visor.tag = "WeakPoint";
            visor.AddComponent<WeakPoint>();
            Child(root, "ShoulderCannon", PrimitiveType.Cube, new Vector3(0.45f, 1.9f, 0.1f), new Vector3(0.25f, 0.25f, 0.7f), armor, keepCollider: false);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0.45f, 1.9f, 0.5f);

            var shieldGo = Child(root, "EnergyShield", PrimitiveType.Cube, new Vector3(-0.05f, 1.3f, 0.75f), new Vector3(1.3f, 2.1f, 0.08f),
                Mat("Shield_Energy", new Color(0.3f, 0.8f, 1f, 0.35f), new Color(0.2f, 0.8f, 1.5f), transparent: true));
            var shield = shieldGo.AddComponent<EnergyShield>();
            SetRef(shield, "shieldRenderer", shieldGo.GetComponent<Renderer>());

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.55f;
            agent.height = 2.3f;
            agent.speed = stats.patrolSpeed;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = 0.5f;
            SetHealth(root.AddComponent<Health>(), 180f);

            var attack = root.AddComponent<RangedBurstAttack>();
            var aso = new SerializedObject(attack);
            aso.FindProperty("muzzle").objectReferenceValue = muzzle;
            aso.FindProperty("boltPrefab").objectReferenceValue = plasma;
            aso.FindProperty("hitMask").intValue = notEnemy;
            aso.FindProperty("boltsPerBurst").intValue = 1;
            aso.FindProperty("damagePerBolt").floatValue = 20f;
            aso.FindProperty("timeBetweenBursts").floatValue = 2.5f;
            aso.FindProperty("telegraphTime").floatValue = 0.6f;
            aso.FindProperty("spreadDegrees").floatValue = 1f;
            aso.ApplyModifiedPropertiesWithoutUndo();

            SetupAI(root.AddComponent<EnemyAI>(), stats, visor.transform, visor.GetComponent<Renderer>());
            return SavePrefab(root, $"{PrefabFolder}/Enemies/AegisWarden.prefab");
        }

        static GameObject SaveTitan()
        {
            var orbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbGo.name = "PlasmaOrb";
            orbGo.layer = enemyLayer;
            orbGo.transform.localScale = Vector3.one * 0.5f;
            orbGo.GetComponent<Renderer>().sharedMaterial = Mat("Boss_Orb", new Color(1f, 0.2f, 0.8f), new Color(3f, 0.4f, 2.5f));
            orbGo.AddComponent<BossOrb>();
            var orb = SavePrefab(orbGo, $"{PrefabFolder}/Enemies/PlasmaOrb.prefab").GetComponent<BossOrb>();

            var root = new GameObject("Unit00_Titan");
            root.layer = enemyLayer;
            var hull = Mat("Boss_Hull", new Color(0.3f, 0.3f, 0.28f), Color.black);
            var body = Child(root, "Body", PrimitiveType.Capsule, new Vector3(0, 2.4f, 0), new Vector3(2.2f, 2.4f, 2.2f), hull);
            body.AddComponent<DamageBlocker>();
            var head = Child(root, "Head", PrimitiveType.Cube, new Vector3(0, 5f, 0.2f), new Vector3(1.2f, 0.9f, 1.2f), hull);
            head.AddComponent<DamageBlocker>();
            Child(root, "Eye", PrimitiveType.Cube, new Vector3(0, 5.05f, 0.82f), new Vector3(0.8f, 0.15f, 0.05f),
                Mat("Boss_Eye", new Color(1f, 0.1f, 0.05f), new Color(4f, 0.2f, 0.1f)), keepCollider: false);

            var shieldGo = Child(root, "RiotShield", PrimitiveType.Cube, new Vector3(0, 2.2f, 1.6f), new Vector3(3f, 3.5f, 0.25f),
                Mat("Boss_RiotShield", new Color(0.25f, 0.35f, 0.5f), new Color(0.1f, 0.3f, 0.6f)));
            var shield = shieldGo.AddComponent<EnergyShield>();
            SetRef(shield, "shieldRenderer", shieldGo.GetComponent<Renderer>());

            var launcher = new GameObject("OrbLauncher").transform;
            launcher.SetParent(root.transform, false);
            launcher.localPosition = new Vector3(0, 4.6f, 1.3f);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 1.2f;
            agent.height = 5.5f;
            agent.speed = 1.2f;
            agent.angularSpeed = 60f;
            SetHealth(root.AddComponent<Health>(), 500f);
            var boss = root.AddComponent<BossController>();
            var so = new SerializedObject(boss);
            so.FindProperty("riotShield").objectReferenceValue = shield;
            so.FindProperty("orbLauncher").objectReferenceValue = launcher;
            so.FindProperty("orbPrefab").objectReferenceValue = orb;
            so.ApplyModifiedPropertiesWithoutUndo();

            var ventMat = Mat("Boss_Vent", new Color(0.3f, 0.9f, 1f), new Color(0.5f, 2.5f, 3f));
            foreach (float x in new[] { -1.25f, 1.25f })
            {
                var vent = Child(root, "ShoulderVent", PrimitiveType.Cube, new Vector3(x, 4.25f, -0.1f), new Vector3(0.7f, 0.45f, 0.7f), ventMat);
                SetRef(vent.AddComponent<BossVent>(), "boss", boss);
            }
            // Phase 2: chest core, tether anchor + two lightning tethers, shockwave prefab, Stinger reinforcements
            var core = Child(root, "ChestCore", PrimitiveType.Sphere, new Vector3(0, 3.2f, 1.15f), Vector3.one * 0.8f,
                Mat("Boss_Core", new Color(0.25f, 0.2f, 0.1f), new Color(0.5f, 0.4f, 0.1f)));
            SetRef(core.AddComponent<BossCore>(), "boss", boss);
            var anchor = new GameObject("TetherAnchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0, 4.2f, 0);
            var tetherMat = Mat("Boss_Tether", new Color(0.4f, 0.9f, 1f), new Color(1f, 3f, 4f));
            var tethers = new LightningLine[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject($"Tether{i + 1}");
                go.transform.SetParent(root.transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.widthMultiplier = 0.12f;
                line.sharedMaterial = tetherMat;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tethers[i] = go.AddComponent<LightningLine>();
            }

            var ringGo = new GameObject("ShockwaveRing");
            var ringLine = ringGo.AddComponent<LineRenderer>();
            ringLine.widthMultiplier = 0.35f;
            ringLine.sharedMaterial = Mat("Boss_Shockwave", new Color(1f, 0.6f, 0.2f), new Color(4f, 1.8f, 0.4f));
            ringLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringGo.AddComponent<ShockwaveRing>();
            var ring = SavePrefab(ringGo, $"{PrefabFolder}/Enemies/ShockwaveRing.prefab").GetComponent<ShockwaveRing>();

            var bso = new SerializedObject(boss);
            SetObjects(bso.FindProperty("tethers"), tethers);
            bso.FindProperty("tetherAnchor").objectReferenceValue = anchor;
            bso.FindProperty("coreRenderer").objectReferenceValue = core.GetComponent<Renderer>();
            bso.FindProperty("shockwavePrefab").objectReferenceValue = ring;
            bso.FindProperty("reinforcementPrefab").objectReferenceValue = stinger;
            bso.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Enemies/Unit00_Titan.prefab");
        }

        // ================================================================== menus

        static GameObject BuildMenusPrefab()
        {
            var root = NewCanvas("GameMenus", 100);
            var controller = root.AddComponent<MenuController>();
            AddEventSystem(root.transform);

            var pause = Panel(root.transform, "PausePanel", "PAUSED");
            var resume = MakeButton(pause, "Resume", 0);
            var retryPause = MakeButton(pause, "Retry from Checkpoint", 1);
            var settingsBtn = MakeButton(pause, "Settings", 2);
            var mainPause = MakeButton(pause, "Main Menu", 3);

            var over = Panel(root.transform, "GameOverPanel", "GAME OVER");
            var reason = Label(over, "Reason", "You were killed in action.", 26, new Vector2(0, 150), new Vector2(900, 100));
            reason.color = new Color(1f, 0.4f, 0.35f);
            var retryOver = MakeButton(over, "Retry from Checkpoint", 1);
            var restart = MakeButton(over, "Restart Game", 2);
            var mainOver = MakeButton(over, "Main Menu", 3);

            var victory = Panel(root.transform, "VictoryPanel", "EXTRACTION SUCCESSFUL");
            var stats = Label(victory, "Stats", "", 28, new Vector2(0, 90), new Vector2(700, 220));
            stats.alignment = TextAlignmentOptions.Left;
            var mainVictory = MakeButton(victory, "Main Menu", 3);

            var settings = Panel(root.transform, "SettingsPanel", "SETTINGS");
            BuildSettings(settings);
            var settingsBack = MakeButton(settings, "Back", 4);

            var so = new SerializedObject(controller);
            so.FindProperty("pausePanel").objectReferenceValue = pause.gameObject;
            so.FindProperty("gameOverPanel").objectReferenceValue = over.gameObject;
            so.FindProperty("victoryPanel").objectReferenceValue = victory.gameObject;
            so.FindProperty("settingsPanel").objectReferenceValue = settings.gameObject;
            so.FindProperty("gameOverReason").objectReferenceValue = reason;
            so.FindProperty("victoryStats").objectReferenceValue = stats;
            SetObjects(so.FindProperty("retryButtons"), retryPause, retryOver);
            so.ApplyModifiedPropertiesWithoutUndo();

            OnClick(resume, controller.Resume);
            OnClick(retryPause, controller.RetryFromCheckpoint);
            OnClick(settingsBtn, controller.OpenSettings);
            OnClick(mainPause, controller.QuitToMainMenu);
            OnClick(retryOver, controller.RetryFromCheckpoint);
            OnClick(restart, controller.RestartGame);
            OnClick(mainOver, controller.QuitToMainMenu);
            OnClick(mainVictory, controller.QuitToMainMenu);
            OnClick(settingsBack, controller.CloseSettings);

            return SavePrefab(root, MenusPath);
        }

        static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f);

            var root = NewCanvas("MainMenu", 0);
            var controller = root.AddComponent<MainMenuController>();
            AddEventSystem(root.transform);

            var main = Panel(root.transform, "MainPanel", "PROJECT OUTPOST\n<size=60%>SIGNAL LOST</size>", transparent: true);
            var play = MakeButton(main, "Play", 0);
            var controls = MakeButton(main, "Controls", 1);
            var settingsBtn = MakeButton(main, "Settings", 2);
            var credits = MakeButton(main, "Credits", 3);
            var quit = MakeButton(main, "Quit", 4);

            var controlsPanel = Panel(root.transform, "ControlsPanel", "CONTROLS");
            Label(controlsPanel, "Text",
                "Move  WASD          Look  Mouse\nSprint  Left Shift     Jump  Space\n" +
                "Pulse Fire  Left Click (hold)\nOverload Beam  Hold Right Click, release\n" +
                "Reload  R          Interact  E\nPause  Esc", 26, new Vector2(0, 80), new Vector2(900, 300));
            var controlsBack = MakeButton(controlsPanel, "Back", 4);

            var settingsPanel = Panel(root.transform, "SettingsPanel", "SETTINGS");
            BuildSettings(settingsPanel);
            var settingsBack = MakeButton(settingsPanel, "Back", 4);

            var creditsPanel = Panel(root.transform, "CreditsPanel", "CREDITS");
            CreditsScreenBuilder.Populate(creditsPanel);
            var creditsBack = MakeButton(creditsPanel, "Back", 4);

            var so = new SerializedObject(controller);
            so.FindProperty("mainPanel").objectReferenceValue = main.gameObject;
            so.FindProperty("controlsPanel").objectReferenceValue = controlsPanel.gameObject;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel.gameObject;
            so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            OnClick(play, controller.Play);
            OnClick(controls, controller.ShowControls);
            OnClick(settingsBtn, controller.ShowSettings);
            OnClick(credits, controller.ShowCredits);
            OnClick(quit, controller.Quit);
            OnClick(controlsBack, controller.Back);
            OnClick(settingsBack, controller.Back);
            OnClick(creditsBack, controller.Back);

            EditorSceneManager.SaveScene(scene, MainMenu);
        }

        static void BuildSettings(RectTransform panel)
        {
            var settings = panel.gameObject.AddComponent<SettingsPanel>();
            var so = new SerializedObject(settings);
            so.FindProperty("sensitivity").objectReferenceValue = SliderRow(panel, "Mouse Sensitivity", 0, 0.2f, 3f);
            so.FindProperty("masterVolume").objectReferenceValue = SliderRow(panel, "Master Volume", 1, 0f, 1f);
            so.FindProperty("musicVolume").objectReferenceValue = SliderRow(panel, "Music Volume", 2, 0f, 1f);
            so.FindProperty("sfxVolume").objectReferenceValue = SliderRow(panel, "Effects Volume", 3, 0f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- UI helpers ----

        static GameObject NewCanvas(string name, int sortOrder)
        {
            var root = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        static void AddEventSystem(Transform parent)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(parent, false);
        }

        static RectTransform Panel(Transform parent, string name, string title, bool transparent = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = transparent ? new Color(0, 0, 0, 0) : new Color(0f, 0f, 0f, 0.82f);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var heading = Label(rect, "Title", title, 64, new Vector2(0, 330), new Vector2(1400, 200));
            heading.fontStyle = FontStyles.Bold;
            return rect;
        }

        static TMP_Text Label(RectTransform parent, string name, string text, float size, Vector2 position, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = box;
            return label;
        }

        /// <summary>Buttons stack downward from the middle of the screen; row 0 is the top one.</summary>
        static Button MakeButton(RectTransform parent, string text, int row)
        {
            var go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
            go.name = $"Button_{text}";
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420, 64);
            rect.anchoredPosition = new Vector2(0, 40 - row * 84);
            go.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.25f, 0.95f);
            var label = go.GetComponentInChildren<TMP_Text>();
            label.text = text;
            label.fontSize = 28;
            label.color = Color.white;
            var colors = go.GetComponent<Button>().colors;
            colors.highlightedColor = new Color(0.4f, 0.8f, 1f);
            colors.pressedColor = new Color(0.2f, 0.6f, 0.8f);
            go.GetComponent<Button>().colors = colors;
            return go.GetComponent<Button>();
        }

        static Slider SliderRow(RectTransform parent, string text, int row, float min, float max)
        {
            float y = 160 - row * 80;
            var label = Label(parent, $"{text}_Label", text, 26, new Vector2(-260, y), new Vector2(400, 50));
            label.alignment = TextAlignmentOptions.Right;
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = $"{text}_Slider";
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420, 24);
            rect.anchoredPosition = new Vector2(200, y);
            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            return slider;
        }

        static void OnClick(Button button, UnityAction action) => UnityEventTools.AddPersistentListener(button.onClick, action);

        // ================================================================== island

        static void PopulateIsland()
        {
            var scene = EditorSceneManager.OpenScene(Island);
            var root = ResetRoot();
            var terrain = Terrain.activeTerrain;
            Vector3 At(float x, float z, float up = 0f) => new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, z)) + up, z);
            PlaceMenus();

            // Radio lines
            var radio = Group(root.transform, "Radio");
            RadioVolume(radio, "Radio_Road", At(95, 56), new Vector3(8, 4, 4), "Radio_Road",
                "The freight elevator is north along the dirt road. Bots patrol it – hit the glowing eyes and visors for critical damage.");
            RadioVolume(radio, "Radio_Return", At(125, 108), new Vector3(7, 4, 7), "Radio_Return",
                "You're back up with both fuses! Climb the hill trail north to the radio tower. Fuse 3 is inside the compound.", requireFuses: true);
            RadioVolume(radio, "Radio_Aegis", At(128, 168), new Vector3(10, 5, 4), "Radio_Aegis",
                "That's an Aegis Warden. Its shield stops pulse shots – break it with an Overload Beam or a barrel, or get around it.");
            var uploadLine = RadioEvent(radio, "Radio_UploadDone",
                "Signal's out – a boat is coming! But something huge just woke up at the pier. Take the service road and stop it!");
            var bossLine = RadioEvent(radio, "Radio_BossDown", "You did it! The boat's at the dock – get on board, now!");

            // Checkpoints
            CheckpointVolume(root.transform, "CP3", At(125, 109), 180f, new Vector3(6, 4, 6), Checkpoint.Condition.HasFusesOneAndTwo);
            CheckpointVolume(root.transform, "CP4", At(143, 44), 200f, new Vector3(12, 5, 6), Checkpoint.Condition.UploadComplete);

            // Return trip: reinforcements on the hill trail once Fuses 1 and 2 are held
            var reinforcements = new GameObject("Reinforcements").AddComponent<ProgressGate>();
            reinforcements.transform.SetParent(root.transform, false);
            PlaceEnemy(reinforcements.transform, enforcer, At(121, 145), "Hill_E1", At(119, 138), At(123, 156));
            PlaceEnemy(reinforcements.transform, stinger, At(124, 158), "Hill_S1", At(124, 158), At(118, 150));
            PlaceEnemy(reinforcements.transform, stinger, At(118, 132), "Hill_S2", At(118, 132), At(124, 128));

            // Tower compound: Aegis guard, Fuse 3, supplies
            var compound = Group(root.transform, "TowerCompound");
            PlaceEnemy(compound, aegis, At(130, 184), "Tower_Aegis", At(126, 184), At(135, 186));
            PlaceEnemy(compound, enforcer, At(124, 196), "Tower_E1", At(122, 196), At(128, 199));
            Place(compound, Load("Props/ExplosiveBarrel"), At(132.5f, 181f));
            Place(compound, Load("Props/ExplosiveBarrel"), At(127f, 186.5f));
            Prim(compound, "Fuse3_Crate", PrimitiveType.Cube, At(136, 197, 0.45f), new Vector3(0.9f, 0.9f, 0.9f),
                Mat("Crate_Wood", new Color(0.55f, 0.38f, 0.2f), Color.black));
            PlacePickup(compound, fuse, At(136, 197, 1.1f), "Pickup_Fuse3", null, fuseNumber: 3);
            var locker = Prim(compound, "AmmoLocker", PrimitiveType.Cube, At(141, 196, 1f), new Vector3(0.8f, 2f, 2f),
                Mat("Prop_AmmoLocker", new Color(0.2f, 0.5f, 0.6f), new Color(0.1f, 0.6f, 0.8f)));
            locker.layer = interactLayer;
            locker.AddComponent<AmmoLocker>();
            PlacePickup(compound, medkit, At(120, 182, 0.4f), "Tower_Medkit1", null);
            PlacePickup(compound, medkit, At(139, 183, 0.4f), "Tower_Medkit2", null);

            // Fuse console + wave defense (replaces the greybox console box)
            HideGreybox("FuseConsole");
            var console = Prim(compound, "FuseConsole", PrimitiveType.Cube, At(Hill.x - 3, Hill.y, 0.6f), new Vector3(1.4f, 1.2f, 0.7f),
                Mat("Prop_Console", new Color(0.3f, 0.32f, 0.35f), Color.black));
            console.layer = interactLayer;
            var empty = Mat("Fuse_SlotEmpty", new Color(0.15f, 0.15f, 0.15f), Color.black);
            var slots = new Renderer[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = Prim(console.transform, $"Slot{i + 1}", PrimitiveType.Cylinder, console.transform.position + new Vector3(-0.4f + i * 0.4f, 0.65f, 0),
                    new Vector3(0.18f, 0.08f, 0.18f), empty);
                Object.DestroyImmediate(slot.GetComponent<Collider>());
                slots[i] = slot.GetComponent<Renderer>();
            }
            var upload = new GameObject("SignalUpload").AddComponent<WaveSpawner>();
            upload.transform.SetParent(compound, false);
            var gates = Points(compound, "WaveGate", At(Hill.x - 18, Hill.y), At(Hill.x, Hill.y + 18), At(Hill.x + 14, Hill.y + 14));
            var uso = new SerializedObject(upload);
            SetObjects(uso.FindProperty("gates"), gates);
            uso.FindProperty("compoundCenter").objectReferenceValue = console.transform;
            var waves = uso.FindProperty("waves");
            waves.arraySize = 3;
            SetWave(waves.GetArrayElementAtIndex(0), 0f, enforcer, stinger, stinger);
            SetWave(waves.GetArrayElementAtIndex(1), 30f, enforcer, enforcer, stinger);
            SetWave(waves.GetArrayElementAtIndex(2), 60f, aegis, enforcer, stinger, stinger);
            uso.ApplyModifiedPropertiesWithoutUndo();

            var consoleComp = console.AddComponent<FuseConsole>();
            var cso = new SerializedObject(consoleComp);
            cso.FindProperty("upload").objectReferenceValue = upload;
            SetObjects(cso.FindProperty("fuseSlots"), slots);
            cso.FindProperty("slotFilled").objectReferenceValue = Mat("Pickup_Fuse", new Color(1f, 0.8f, 0.1f), new Color(3f, 2.2f, 0.3f));
            cso.ApplyModifiedPropertiesWithoutUndo();

            // Service gate in the east fence: opens when the upload completes
            var gate = MakeDoor(compound, "ServiceGate", At(Hill.x + 13, Hill.y), new Vector3(0.2f, 2.5f, 4f), new Vector3(0, 0, 4.2f),
                lockedReason: "Gate sealed until the signal upload completes", startsLocked: true,
                unlockedWhenCleared: WaveSpawner.CompletedId, alongZ: true);

            // Boss at the pier, activated by the upload
            var bossGate = new GameObject("Boss").AddComponent<ProgressGate>();
            bossGate.transform.SetParent(root.transform, false);
            var bso = new SerializedObject(bossGate);
            bso.FindProperty("activeFrom").enumValueIndex = (int)ProgressGate.Stage.UploadComplete;
            bso.ApplyModifiedPropertiesWithoutUndo();
            var boss = Place(bossGate.transform, titan, new Vector3(140f, 6.3f, 26f));
            boss.transform.rotation = Quaternion.Euler(0, 0, 0);

            // Phase 2 power pylons on the pier (replace the greybox pylon boxes)
            HideGreybox("Pylon_West");
            HideGreybox("Pylon_East");
            var pylons = new[]
            {
                MakePylon(bossGate.transform, "Pylon_West", new Vector3(131f, 6.3f, 10f)),
                MakePylon(bossGate.transform, "Pylon_East", new Vector3(149f, 6.3f, 10f))
            };
            var reinforcementPoints = Points(bossGate.transform, "BossReinforcement", new Vector3(134f, 6.3f, 32f), new Vector3(146f, 6.3f, 32f));
            var bossSo = new SerializedObject(boss.GetComponent<BossController>());
            SetObjects(bossSo.FindProperty("pylons"), pylons);
            SetObjects(bossSo.FindProperty("reinforcementPoints"), reinforcementPoints);
            bossSo.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(boss.GetComponent<BossController>().onDefeated, new UnityAction(bossLine.Play));

            UnityEventTools.AddPersistentListener(upload.onUploadComplete, new UnityAction(gate.Unlock));
            UnityEventTools.AddPersistentListener(upload.onUploadComplete, new UnityAction(gate.Open));
            UnityEventTools.AddPersistentListener(upload.onUploadComplete, new UnityAction(bossGate.Activate));
            UnityEventTools.AddPersistentListener(upload.onUploadComplete, new UnityAction(uploadLine.Play));

            // Extraction boat (replaces the greybox box)
            HideGreybox("ExtractionBoat");
            var boat = Prim(root.transform, "ExtractionBoat", PrimitiveType.Cube, new Vector3(155.5f, 4.8f, 20f), new Vector3(3f, 2f, 8f),
                Mat("Prop_Boat", new Color(0.9f, 0.45f, 0.1f), new Color(0.3f, 0.12f, 0f)));
            boat.layer = interactLayer;
            boat.AddComponent<ExtractionBoat>();

            EnsurePlayer(At(93, 43));
            EditorSceneManager.SaveScene(scene);
        }

        // ================================================================== bunker

        static void PopulateBunker()
        {
            var scene = EditorSceneManager.OpenScene(Bunker);
            var root = ResetRoot();
            PlaceMenus();

            var radio = Group(root.transform, "Radio");
            RadioVolume(radio, "Radio_Arrive", new Vector3(0, 0, 2), new Vector3(6, 4, 3), "Radio_Arrive",
                "You're in. Wing A is west, Wing B is east – one fuse in each. OVERSEER scrambled the elevator circuit too – the junction box is by the hub entrance.");
            RadioVolume(radio, "Radio_WingA", new Vector3(-29, 0, 20), new Vector3(2, 4, 4), "Radio_WingA",
                "The breaker order should be in the maintenance log near the hub. Get it wrong and security comes running.");
            RadioVolume(radio, "Radio_WingB", new Vector3(14, 0, 20), new Vector3(2, 4, 4), "Radio_WingB",
                "Careful – the floodway is electrified. Use the crates to reach the drain controls on the north wall – the pipe chart shows the way.");

            CheckpointVolume(root.transform, "CP1", new Vector3(0, 0, -1), 0f, new Vector3(6, 4, 5), Checkpoint.Condition.Always);
            CheckpointVolume(root.transform, "CP2", new Vector3(0, 0, 20), 0f, new Vector3(12, 4, 12), Checkpoint.Condition.Always);

            // The Aegis Warden replaces the Recon Stinger in the quarantine lockdown squad
            var lockdown = Object.FindFirstObjectByType<LockdownRoom>();
            if (lockdown != null)
            {
                var lso = new SerializedObject(lockdown);
                lso.FindProperty("squad").GetArrayElementAtIndex(2).objectReferenceValue = aegis;
                lso.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsurePlayer(new Vector3(0, 0, -1));
            EditorSceneManager.SaveScene(scene);
        }

        // ================================================================== placement helpers

        static GameObject ResetRoot()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var existingMenus = GameObject.Find("GameMenus");
            if (existingMenus != null) Object.DestroyImmediate(existingMenus);
            return new GameObject(RootName);
        }

        static void PlaceMenus() => PrefabUtility.InstantiatePrefab(menus);

        static void HideGreybox(string name)
        {
            var go = GameObject.Find(name);
            if (go != null && go.transform.root.name == "Greybox") go.SetActive(false);
        }

        static void RadioVolume(Transform parent, string name, Vector3 floor, Vector3 size, string id, string line, bool requireFuses = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = floor + Vector3.up * size.y / 2f;
            go.AddComponent<BoxCollider>().size = size;
            var trigger = go.AddComponent<RadioTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("line").stringValue = line;
            so.FindProperty("persistentId").stringValue = id;
            so.FindProperty("requireFusesOneAndTwo").boolValue = requireFuses;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static RadioTrigger RadioEvent(Transform parent, string id, string line)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent, false);
            var trigger = go.AddComponent<RadioTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("line").stringValue = line;
            so.FindProperty("persistentId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return trigger;
        }

        static void CheckpointVolume(Transform parent, string id, Vector3 floor, float yaw, Vector3 size, Checkpoint.Condition condition)
        {
            var go = new GameObject($"Checkpoint_{id}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(floor, Quaternion.Euler(0, yaw, 0));
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.center = Vector3.up * size.y / 2f;
            var spawn = go.AddComponent<SpawnPoint>();
            var sso = new SerializedObject(spawn);
            sso.FindProperty("id").stringValue = id;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var cp = go.AddComponent<Checkpoint>();
            var cso = new SerializedObject(cp);
            cso.FindProperty("condition").enumValueIndex = (int)condition;
            cso.ApplyModifiedPropertiesWithoutUndo();
        }

        static BossPylon MakePylon(Transform parent, string name, Vector3 floor)
        {
            var root = new GameObject(name);
            root.layer = enemyLayer;
            root.transform.SetParent(parent, false);
            root.transform.position = floor;
            var column = Child(root, "Column", PrimitiveType.Cube, new Vector3(0, 3f, 0), new Vector3(1f, 6f, 1f),
                Mat("Pylon_Column", new Color(0.3f, 0.32f, 0.35f), Color.black));
            var glowMat = Mat("Pylon_Glow", new Color(0.4f, 0.9f, 1f), new Color(1f, 3f, 4f));
            var band = Child(root, "GlowBand", PrimitiveType.Cube, new Vector3(0, 3f, 0), new Vector3(1.05f, 0.4f, 1.05f), glowMat, keepCollider: false);
            var top = Child(root, "GlowTop", PrimitiveType.Sphere, new Vector3(0, 6.3f, 0), Vector3.one * 0.8f, glowMat);
            var tetherPoint = new GameObject("TetherPoint").transform;
            tetherPoint.SetParent(root.transform, false);
            tetherPoint.localPosition = new Vector3(0, 6.3f, 0);
            var health = root.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").floatValue = 100f;
            hso.FindProperty("destroyOnDeath").boolValue = false;
            hso.ApplyModifiedPropertiesWithoutUndo();
            var pylon = root.AddComponent<BossPylon>();
            var pso = new SerializedObject(pylon);
            SetObjects(pso.FindProperty("glowRenderers"), band.GetComponent<Renderer>(), top.GetComponent<Renderer>());
            pso.FindProperty("tetherPoint").objectReferenceValue = tetherPoint;
            pso.ApplyModifiedPropertiesWithoutUndo();
            column.name = "Column";
            return pylon;
        }

        static void SetWave(SerializedProperty wave, float atSecond, params GameObject[] enemies)
        {
            wave.FindPropertyRelative("atSecond").floatValue = atSecond;
            SetObjects(wave.FindPropertyRelative("enemies"), enemies);
        }
    }
}
