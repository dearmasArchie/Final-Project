using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Automated check of the start of a run: enters Play mode in MainMenu, presses Play, waits for the
    /// island to load, then reports the scene, the player, the HUD, and any errors to Library/SignalLost/bridge.log.
    /// Run with the "smoketest" bridge command.
    /// </summary>
    [InitializeOnLoad]
    public static class PlaySmokeTest
    {
        const string ActiveKey = "SignalLost.SmokeTest.Active";
        const string StartKey = "SignalLost.SmokeTest.Start";
        const string StepKey = "SignalLost.SmokeTest.Step";
        static readonly string LogPath = Path.Combine("Library/SignalLost", "bridge.log");
        static readonly List<string> errors = new List<string>();

        static PlaySmokeTest()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            // Keep Play mode running while the Editor window is in the background.
            PlayerSettings.runInBackground = true;
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetFloat(StartKey, -1f);
            SessionState.SetInt(StepKey, 0);
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                if (errors.Count < 10) errors.Add(message.Split('\n')[0]);
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            float now = (float)EditorApplication.timeSinceStartup;
            float start = SessionState.GetFloat(StartKey, -1f);
            if (start < 0f)
            {
                SessionState.SetFloat(StartKey, now);
                return;
            }

            float t = now - start;
            int step = SessionState.GetInt(StepKey, 0);

            if (step == 0 && t > 1.5f)
            {
                var menu = Object.FindFirstObjectByType<MainMenuController>();
                Write($"smoketest: scene={SceneManager.GetActiveScene().name}, main menu found={menu != null}");
                if (menu != null) menu.PlayImmediate();
                SessionState.SetInt(StepKey, 1);
            }
            else if (step == 1 && (SceneManager.GetActiveScene().name != "MainMenu" || t > 30f))
            {
                Write($"smoketest: scene changed after {t - 1.5f:0.0}s, fade loading={SceneLoader.Instance.IsLoading}, frame={Time.frameCount}");
                SessionState.SetInt(StepKey, 2);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 2 && t > 2f)
            {
                // Talk to Dr. Reyes, then press E through the briefing.
                var reyes = GameObject.Find("Dr_Reyes");
                var player = GameObject.FindGameObjectWithTag("Player");
                if (reyes != null && player != null) reyes.GetComponent<DialogueSource>().Interact(player);
                var calm = Object.FindFirstObjectByType<StormController>();
                Write($"storm: at game start intensity={(calm != null ? calm.Intensity.ToString("0.00") : "NO CONTROLLER")}, sun={(calm != null ? calm.SunIntensity.ToString("0.00") : "?")}");
                SaveCameraPng("Library/SignalLost/storm_clear.png", new Vector3(108f, 34f, 172f), new Vector3(130f, 31f, 192f));
                var reyesAnimator = reyes != null ? reyes.GetComponentInChildren<Animator>() : null;
                if (reyesAnimator != null)
                {
                    var hipsBone = reyesAnimator.GetBoneTransform(HumanBodyBones.Hips);
                    var info = reyesAnimator.GetCurrentAnimatorStateInfo(0);
                    Write($"reyes: animator playing={reyesAnimator.isActiveAndEnabled}, state SittingIdle={info.IsName("SittingIdle")}, time={info.normalizedTime:0.00}, " +
                          $"hips {(hipsBone != null ? (hipsBone.position.y - reyes.transform.position.y + 0.7f).ToString("0.00") : "?")} m above the floor, skinned parts={reyes.GetComponentsInChildren<SkinnedMeshRenderer>().Length}");
                }
                else Write("reyes: NO animator found (model missing)");
                Write($"smoketest: briefing opened={reyes != null && player != null}");
                SessionState.SetInt(StepKey, 3);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step >= 3 && step < 13 && t > 0.3f)
            {
                // Odd steps press E, even steps release it: five presses for five lines.
                var keyboard = Keyboard.current;
                if (keyboard != null)
                    InputSystem.QueueStateEvent(keyboard, step % 2 == 1 ? new KeyboardState(Key.E) : new KeyboardState());
                SessionState.SetInt(StepKey, step + 1);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 13 && t > 1f)
            {
                var door = GameObject.Find("ArmoryDoor");
                Write($"smoketest: briefing done={GameManager.Instance.IsCleared("Briefing")}, armory still locked={(door != null && door.GetComponent<Door>().IsLocked)}, " +
                      $"code={GameManager.Instance.ArmoryPuzzle.Code}");
                // Open the keypad and type a wrong code first.
                var keypad = Object.FindFirstObjectByType<CodeKeypad>();
                keypad.Interact(GameObject.FindGameObjectWithTag("Player"));
                string wrong = GameManager.Instance.ArmoryPuzzle.Code == "0000" ? "1111" : "0000";
                foreach (char c in wrong) InputSystem.QueueTextEvent(Keyboard.current, c);
                SessionState.SetInt(StepKey, 20);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 20 && t > 0.5f)
            {
                Write($"smoketest: after wrong code unlocked={GameManager.Instance.IsCleared(CodeKeypad.UnlockedId)}");
                foreach (char c in GameManager.Instance.ArmoryPuzzle.Code) InputSystem.QueueTextEvent(Keyboard.current, c);
                SessionState.SetInt(StepKey, 21);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 21 && t > 2f)
            {
                var door = GameObject.Find("ArmoryDoor").GetComponent<Door>();
                var bunker = Object.FindFirstObjectByType<ArmoryCodePuzzle>();
                Write($"smoketest: after right code unlocked={GameManager.Instance.IsCleared(CodeKeypad.UnlockedId)}, door open={door.IsOpen}, " +
                      $"bunker puzzle present={bunker != null} at {GameObject.Find("Locker_Blue")?.transform.position}, player can move={GameObject.FindGameObjectWithTag("Player").GetComponent<FirstPersonController>().enabled}");
                SessionState.SetInt(StepKey, 30);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 30 && t > 0.3f)
            {
                ReportBunker();
                ReportPower();
                // Ride down to test the underground puzzles
                GameManager.Instance.NextSpawnPointId = "ElevatorBottom";
                SceneLoader.Instance.Load("UndergroundFacility");
                SessionState.SetInt(StepKey, 40);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 40 && SceneManager.GetActiveScene().name == "UndergroundFacility" && t > 2f)
            {
                var gm = GameManager.Instance;
                var readings = gm.GaugeReadings;
                var breakerDoor = GameObject.Find("Door_BreakerRoom").GetComponent<Door>();
                Write($"underground: loaded, gauges={readings[0]}{readings[1]}{readings[2]}, breaker door locked={breakerDoor.IsLocked}");
                var keypad = GameObject.Find("BreakerRoomKeypad").GetComponent<CodeKeypad>();
                keypad.Interact(GameObject.FindGameObjectWithTag("Player"));
                foreach (char c in $"{readings[0]}{readings[1]}{readings[2]}") InputSystem.QueueTextEvent(Keyboard.current, c);
                SessionState.SetInt(StepKey, 41);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 41 && t > 2f)
            {
                var gm = GameManager.Instance;
                var breakerDoor = GameObject.Find("Door_BreakerRoom").GetComponent<Door>();
                Write($"gauge lock: unlocked={gm.IsCleared(CodeKeypad.BreakerRoomUnlockedId)}, door open={breakerDoor.IsOpen}");

                var routing = Object.FindFirstObjectByType<PipeRouting>();
                var qDoor = GameObject.Find("Door_Quarantine").GetComponent<Door>();
                var solution = routing.Solution;
                routing.SetLeversAndTest(new[] { !solution[0], solution[1], solution[2] });
                bool drainedAfterWrong = routing.Drained;
                routing.SetLeversAndTest(solution);
                Write($"pipe routing: drained after wrong={drainedAfterWrong}, after correct={routing.Drained}, quarantine door unlocked={!qDoor.IsLocked}");

                var wiring = Object.FindFirstObjectByType<WiringPuzzle>();
                var portal = GameObject.Find("ElevatorPanel_Up").GetComponent<ScenePortal>();
                gm.CollectFuse(1);
                gm.CollectFuse(2);
                bool startsSolved = wiring.Solved;
                bool beforeCircuit = portal.CanInteract;
                string promptBefore = portal.Prompt;
                wiring.SolveForTest();
                Write($"junction box: starts solved={startsSolved}, in row={wiring.InputRow} out row={wiring.OutputRow}, " +
                      $"elevator up before={beforeCircuit} (\"{promptBefore}\"), after solve={wiring.Solved}/{portal.CanInteract}");

                // Jump ahead to the boss: mark the upload done and ride back up
                gm.MarkCleared(WaveSpawner.CompletedId);
                gm.NextSpawnPointId = "ElevatorTop";
                SceneLoader.Instance.Load("IslandExterior");
                SessionState.SetInt(StepKey, 50);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 50 && SceneManager.GetActiveScene().name == "IslandExterior" && t > 2f)
            {
                var boss = Object.FindFirstObjectByType<BossController>();
                if (boss == null) { Write("boss: NOT ACTIVE after upload"); SessionState.SetInt(StepKey, 99); return; }
                float hpStart = boss.Health.Current;
                boss.ForcePhase2ForTest();
                boss.CheckPhaseForTest();
                float hpPhase2 = boss.Health.Current;
                // Invulnerable while tethered: vents and core do nothing
                foreach (var vent in boss.GetComponentsInChildren<BossVent>()) vent.TakeDamage(60f, DamageType.Overload, vent.transform.position);
                boss.GetComponentInChildren<BossCore>().TakeDamage(60f, DamageType.Pulse, boss.transform.position);
                Write($"boss: hp start={hpStart}, phase2={boss.InPhase2} at {hpPhase2}, after hits while tethered={boss.Health.Current} (should be {hpPhase2})");
                // Stand the player on the hill by the radio tower, looking at it, so the rain frame shows the weather from the player's eyes
                var hillPlayer = GameObject.FindGameObjectWithTag("Player");
                if (hillPlayer != null)
                {
                    var controller = hillPlayer.GetComponent<CharacterController>();
                    controller.enabled = false;
                    hillPlayer.transform.position = new Vector3(112f, Terrain.activeTerrain.SampleHeight(new Vector3(112f, 0f, 178f)) + Terrain.activeTerrain.transform.position.y + 0.2f, 178f);
                    hillPlayer.transform.rotation = Quaternion.LookRotation(new Vector3(130f - 112f, 0f, 192f - 178f));
                    controller.enabled = true;
                }
                SessionState.SetInt(StepKey, 51);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 51 && t > 2.5f)
            {
                var storm = Object.FindFirstObjectByType<StormController>();
                Write($"storm: in the boss fight intensity={storm?.Intensity:0.00}, sun={storm?.SunIntensity:0.00} (clear was higher), rain particles={storm?.RainParticles}, fog start={RenderSettings.fogStartDistance:0}, fog end={RenderSettings.fogEndDistance:0}");
                SaveCameraPng("Library/SignalLost/storm_on.png");
                var boss = Object.FindFirstObjectByType<BossController>();
                int rings = Object.FindObjectsByType<ShockwaveRing>(FindObjectsSortMode.None).Length;
                var pylons = Object.FindObjectsByType<BossPylon>(FindObjectsSortMode.None);
                foreach (var pylon in pylons) pylon.GetComponent<Health>().TakeDamage(200f, DamageType.Pulse, pylon.transform.position);
                float before = boss.Health.Current;
                boss.GetComponentInChildren<BossCore>().TakeDamage(20f, DamageType.Pulse, boss.transform.position);
                Write($"boss: shockwave rings seen={rings}, pylons={pylons.Length} destroyed → stunned={boss.Stunned}, core exposed={boss.CoreExposed}, " +
                      $"core hit 20 → hp {before} → {boss.Health.Current}");
                SessionState.SetInt(StepKey, 52);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 52 && t > 5.5f)
            {
                var boss = Object.FindFirstObjectByType<BossController>();
                var pylons = Object.FindObjectsByType<BossPylon>(FindObjectsSortMode.None);
                int up = 0;
                foreach (var pylon in pylons) if (!pylon.IsDown) up++;
                Write($"boss: after stun → stunned={boss.Stunned}, pylons back up={up}/{pylons.Length} at {pylons[0].GetComponent<Health>().Current} HP, " +
                      $"post-processing volume={Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>() != null}");
                SessionState.SetInt(StepKey, 53);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 53 && t > 0.3f)
            {
                // Titan gait: let it walk toward the player and read its hip and knee angles over time
                var boss = Object.FindFirstObjectByType<BossController>();
                var agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
                var player = GameObject.FindGameObjectWithTag("Player");
                agent.isStopped = false;
                agent.speed = 1.2f;
                agent.SetDestination(player.transform.position);
                SessionState.SetInt(StepKey, 54);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step >= 54 && step <= 56 && t > 0.6f)
            {
                var boss = Object.FindFirstObjectByType<BossController>();
                var agent = boss.GetComponent<UnityEngine.AI.NavMeshAgent>();
                Transform Find(string n) { foreach (var tr in boss.GetComponentsInChildren<Transform>(true)) if (tr.name == n) return tr; return null; }
                float Pitch(Transform tr) { float a = tr.localEulerAngles.x; return a > 180f ? a - 360f : a; }
                Write($"titan gait {step - 53}: speed={agent.velocity.magnitude:0.00}, hips L={Pitch(Find("LegL_Hip")):0.0} R={Pitch(Find("LegR_Hip")):0.0}, " +
                      $"knees L={Pitch(Find("LegL_Knee")):0.0} R={Pitch(Find("LegR_Knee")):0.0}, ankles L={Pitch(Find("LegL_Ankle")):0.0} R={Pitch(Find("LegR_Ankle")):0.0}");
                SessionState.SetInt(StepKey, step == 56 ? 70 : step + 1);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 70 && t > 0.3f)
            {
                var sway = Object.FindFirstObjectByType<GunSway>(FindObjectsInactive.Include);
                gunSway = sway;
                gunMin = Vector3.one * 99f; gunMax = Vector3.one * -99f; gunPeakBack = 0f;
                Write($"gun: component found={sway != null}, model active={(sway != null && sway.gameObject.activeInHierarchy)}");
                SessionState.SetInt(StepKey, sway == null ? 57 : 71);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 71)
            {
                // Walk the player forward for 1.5 s and record how far the gun moves
                var player = GameObject.FindGameObjectWithTag("Player");
                player.GetComponent<CharacterController>().Move(player.transform.forward * 4.5f * Time.deltaTime + Vector3.down * 0.05f);
                if (t > 0.3f) { gunMin = Vector3.Min(gunMin, gunSway.CurrentOffset); gunMax = Vector3.Max(gunMax, gunSway.CurrentOffset); }
                if (t > 1.5f)
                {
                    Write($"gun: walking bob range x {(gunMax.x - gunMin.x) * 100:0.0} cm, y {(gunMax.y - gunMin.y) * 100:0.0} cm, z {(gunMax.z - gunMin.z) * 100:0.0} cm (should be a few cm, not zero)");
                    gunSway.GetComponentInParent<ArcPistol>().RaiseShotForTest(false);
                    SessionState.SetInt(StepKey, 72);
                    SessionState.SetFloat(StartKey, now);
                }
            }
            else if (step == 72)
            {
                gunPeakBack = Mathf.Min(gunPeakBack, gunSway.CurrentOffset.z);
                if (t > 1.6f)
                {
                    Write($"gun: recoil peak back {-gunPeakBack * 100:0.0} cm after a shot, offset 1.6 s later {gunSway.CurrentOffset.magnitude * 100:0.0} cm (idle breathing only)");
                    SessionState.SetInt(StepKey, 57);
                    SessionState.SetFloat(StartKey, now);
                }
            }
            else if (step == 57 && t > 0.5f)
            {
                // Kill one of each enemy type in the scene and watch what their animators do afterwards
                deathTargets.Clear();
                var seen = new HashSet<string>();
                foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
                {
                    string key = ai.name.Replace("(Clone)", "").Trim();
                    if (!seen.Add(key) || !ai.TryGetComponent<Health>(out var hp)) continue;
                    deathTargets.Add((key, ai.GetComponentInChildren<Animator>(), ai.transform));
                    hp.TakeDamage(99999f, DamageType.Hazard, ai.transform.position);
                }
                Write($"death: killed {deathTargets.Count} enemy types ({string.Join(", ", deathTargets.Select(d => d.name))})");
                SessionState.SetInt(StepKey, 58);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 58 && t > 1.3f)
            {
                SampleDeaths("1.3 s after death");
                SessionState.SetInt(StepKey, 59);
            }
            else if (step == 59 && t > 2.6f)
            {
                SampleDeaths("2.6 s after death");
                SessionState.SetInt(StepKey, 80);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 80 && t > 0.3f)
            {
                // Win: the victory screen opens with its rank and stats, and the winning song plays through the pause
                MenuController.Instance.ShowVictory();
                SessionState.SetInt(StepKey, 81);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 81 && t > 0.8f)
            {
                var victoryPanel = MenuController.Instance.transform.Find("VictoryPanel");
                var rankText = victoryPanel != null ? victoryPanel.Find("Rank")?.GetComponent<TMPro.TMP_Text>() : null;
                var statsText = victoryPanel != null ? victoryPanel.Find("Stats")?.GetComponent<TMPro.TMP_Text>() : null;
                Write($"victory: panel shown={victoryPanel != null && victoryPanel.gameObject.activeSelf}, rank=\"{rankText?.text}\", stats first line=\"{statsText?.text.Split('\n')[0]}\", " +
                      $"song playing={MusicManager.VictoryPlaying}, game paused={Time.timeScale == 0f}");
                MusicManager.StopVictory(); // what leaving the screen does (the real buttons also load a scene)
                Time.timeScale = 1f;
                AudioListener.pause = false;
                SessionState.SetInt(StepKey, 82);
                SessionState.SetFloat(StartKey, now);
            }
            else if (step == 82 && t > 0.5f)
            {
                Write($"victory: after leaving, song playing={MusicManager.VictoryPlaying} (should be False)");
                SessionState.SetInt(StepKey, 99);
            }
            else if (step == 99)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                Write($"smoketest: after Play scene={SceneManager.GetActiveScene().name}, player={(player != null ? player.transform.position.ToString() : "none")}, " +
                      $"HUD={HUDController.Instance != null}, menus={MenuController.Instance != null}, timer={GameManager.Instance.TimeRemaining:0}, timeScale={Time.timeScale}");
                ReportTrees();
                ReportAudio();
                Write(errors.Count == 0 ? "smoketest: no errors" : "smoketest: ERRORS\n  " + string.Join("\n  ", errors));
                Write("done smoketest");
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.update -= Tick;
                Application.logMessageReceived -= OnLog;
                EditorApplication.ExitPlaymode();
            }
        }

        static void SaveCameraPng(string path, Vector3? at = null, Vector3? lookAt = null)
        {
            var cam = Camera.main;
            if (cam == null) { Write("storm: no main camera to capture"); return; }
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation;
            if (at.HasValue) { cam.transform.position = at.Value; cam.transform.LookAt(lookAt ?? at.Value + Vector3.forward); }
            var rt = new RenderTexture(1280, 720, 24);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = previous;
            cam.transform.SetPositionAndRotation(savedPos, savedRot);
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
        }

        static GunSway gunSway;
        static Vector3 gunMin, gunMax;
        static float gunPeakBack;
        static readonly List<(string name, Animator animator, Transform body)> deathTargets = new List<(string, Animator, Transform)>();

        static void SampleDeaths(string when)
        {
            int die = Animator.StringToHash("Die");
            foreach (var (name, animator, body) in deathTargets)
            {
                if (body == null) { Write($"death: {name} {when}: already removed"); continue; }
                if (animator == null) { Write($"death: {name} {when}: no animator, y={body.position.y:0.00}"); continue; }
                var info = animator.GetCurrentAnimatorStateInfo(0);
                int shieldsVisible = 0;
                foreach (var shield in body.GetComponentsInChildren<EnergyShield>(true))
                    foreach (var r in shield.GetComponentsInChildren<Renderer>(true)) if (r.enabled) shieldsVisible++;
                Write($"death: {name} {when}: animator on={animator.enabled}, speed={animator.speed}, in Die state={info.shortNameHash == die}, " +
                      $"normalized time={info.normalizedTime:0.00}, y={body.position.y:0.00}" + (name.Contains("Aegis") ? $", shield renderers still visible={shieldsVisible} (should be 0)" : ""));
            }
        }

        /// <summary>The library loads, the mixer volumes are exposed, music is running, and the player has footsteps.</summary>
        static void ReportAudio()
        {
            var library = Sfx.Library;
            if (library == null) { Write("audio: NO LIBRARY (run the audio command)"); return; }
            bool music = library.mixer.SetFloat(Sfx.MusicParam, -3f);
            bool sfx = library.mixer.SetFloat(Sfx.SfxParam, 0f);
            Sfx.ApplySavedVolumes();
            var player = GameObject.FindGameObjectWithTag("Player");
            var source = Object.FindFirstObjectByType<MusicManager>();
            int playing = 0;
            if (source != null) foreach (var a in source.GetComponents<AudioSource>()) if (a.isPlaying) playing++;
            Sfx.Play2D("pulse_fire");
            Write($"audio: sounds={library.sounds.Length}, mixer music={music} sfx={sfx}, music manager={source != null} tracks playing={playing}, " +
                  $"footsteps={(player != null && player.GetComponent<FootstepPlayer>() != null)}, bots with servo={Object.FindObjectsByType<EnemyAudio>(FindObjectsSortMode.None).Length}");
        }

        /// <summary>Every terrain tree should stop the player: a chest-height ray at each trunk must hit the terrain's tree collider.</summary>
        static void ReportTrees()
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return;
            var data = terrain.terrainData;
            var hits = new int[data.treePrototypes.Length];
            var totals = new int[data.treePrototypes.Length];
            foreach (var tree in data.treeInstances)
            {
                Vector3 trunk = Vector3.Scale(tree.position, data.size) + terrain.transform.position;
                totals[tree.prototypeIndex]++;
                if (Physics.Raycast(trunk + new Vector3(3f, 1.2f, 0f), Vector3.left, out var hit, 3.5f) && hit.collider is TerrainCollider)
                    hits[tree.prototypeIndex]++;
            }
            var parts = new string[hits.Length];
            for (int i = 0; i < hits.Length; i++) parts[i] = $"{data.treePrototypes[i].prefab.name} {hits[i]}/{totals[i]}";
            Write($"trees: solid trunks = {string.Join(", ", parts)}");
        }

        /// <summary>Checks the maintenance bunker wherever it stands: grounded, in one piece, switch works, out of enemy sight.</summary>
        static void ReportBunker()
        {
            var puzzle = Object.FindFirstObjectByType<ArmoryCodePuzzle>();
            if (puzzle == null) { Write("bunker: NOT FOUND"); return; }
            var root = puzzle.transform;
            var floor = root.Find("Floor");
            var terrain = Terrain.activeTerrain;
            float ground = terrain.SampleHeight(floor.position) + terrain.transform.position.y;
            Write($"bunker: root pos={root.position} rot={root.eulerAngles.y:0}, floor={floor.position}, ground under floor={ground:0.00} (gap {floor.position.y + 0.1f - ground:0.00} m)");

            // Parts should still sit around the floor (nothing left behind if only some objects were moved)
            float worst = 0f; string worstName = "";
            foreach (Transform child in root)
            {
                float d = Vector3.Distance(new Vector3(child.position.x, 0, child.position.z), new Vector3(floor.position.x, 0, floor.position.z));
                if (d > worst) { worst = d; worstName = child.name; }
            }
            Write($"bunker: farthest part from floor center = {worstName} at {worst:0.0} m (bunker half-diagonal is ~5.4 m)");

            // Light switch reveals the hidden digit
            var hidden = GameManager.Instance.ArmoryPuzzle.hiddenColor;
            string glowName = $"GlowDigit_{GameManager.ArmoryPuzzleData.ColorNames[hidden][0]}{GameManager.ArmoryPuzzleData.ColorNames[hidden].Substring(1).ToLower()}";
            Transform glow = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == glowName) { glow = t; break; }   // lives under PaintClues
            var lightSwitch = root.GetComponentInChildren<LightSwitch>();
            var player = GameObject.FindGameObjectWithTag("Player");
            bool before = glow != null && glow.gameObject.activeSelf;
            lightSwitch.Interact(player);
            bool off = glow != null && glow.gameObject.activeSelf;
            lightSwitch.Interact(player);
            bool after = glow != null && glow.gameObject.activeSelf;
            Write($"bunker: glow digit visible lights-on={before}, lights-off={off}, lights-on-again={after}");

            // Distance to the closest enemy or patrol waypoint, and to the safe room door
            float nearest = float.MaxValue; string nearestName = "";
            foreach (var enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(enemy.transform.position, floor.position);
                if (d < nearest) { nearest = d; nearestName = enemy.name; }
            }
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("WP")) continue;
                float d = Vector3.Distance(t.position, floor.position);
                if (d < nearest) { nearest = d; nearestName = $"{t.parent.name}/{t.name}"; }
            }
            Write($"bunker: nearest enemy/patrol point = {nearestName} at {nearest:0.0} m (vision range 15 m); " +
                  $"distance from safe room door = {Vector3.Distance(floor.position, new Vector3(93, floor.position.y, 50)):0} m");
        }

        /// <summary>Elevator stays locked until the generator bunker's main power switch is on.</summary>
        static void ReportPower()
        {
            GameManager.Instance.HasArcPistol = true;
            var portal = GameObject.Find("ElevatorPanel_Down").GetComponent<ScenePortal>();
            bool before = portal.CanInteract;
            string promptBefore = portal.Prompt;
            var power = Object.FindFirstObjectByType<PowerSwitch>();
            int guards = 0;
            foreach (var enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
                if (enemy.name.Contains("Gen_")) guards++;
            power.Interact(GameObject.FindGameObjectWithTag("Player"));
            Write($"power: elevator usable before switch={before} (\"{promptBefore}\"), after switch={portal.CanInteract} (\"{portal.Prompt}\"), " +
                  $"generator guards={guards}, switch at {power.transform.position}");
        }

        static void Write(string line) =>
            File.AppendAllText(LogPath, $"{System.DateTime.Now:HH:mm:ss} {line}{System.Environment.NewLine}");
    }
}
