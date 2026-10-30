# Signal Lost – Development Roadmap

The full design is in [GameConcept.md](GameConcept.md). This file breaks the build into phases. Each phase ends with a **Done when** test: don't start the next phase until it passes.

**How to use this file**
* Tick a box by changing `- [ ]` to `- [x]` and committing. GitHub shows the boxes as checkboxes.
* **A** = Partner A (gameplay: player, weapon, enemies, boss). **B** = Partner B (world & systems: scenes, GameManager, interactions, puzzles, UI, audio). **Both** = do it together.
* Day numbers are targets. If a phase runs late, use the cut list at the bottom instead of skipping the Done-when test.

---

## Phase 0 – Setup (Day 1 morning, ~2 hours)

Goal: both partners can open the same project, and the shared code contracts are agreed before anyone writes gameplay code.

### Both
- [ ] Install the same Unity version (6000.4.0f1) on both PCs
- [ ] Make the first commit on `main` and push it to GitHub
- [ ] Partner A clones the repo and opens the project without errors
- [ ] Agree the Git rules: each partner works on their own branch, merges at the end of every day, and never edits the same scene at the same time
- [ ] Read through GameConcept.md together (15 min) and agree on any changes
- [ ] Agree and commit the shared interfaces before splitting up:
  - [ ] `IInteractable` (Prompt, CanInteract, Interact)
  - [ ] `IDamageable` (TakeDamage with amount, DamageType, hit point)
  - [ ] `DamageType` enum (Pulse, Overload, Explosion, Hazard)
  - [ ] `GameManager` public API (what A can call: player HP/ammo save and load, enemy-killed events)
- [ ] Agree layers and tags (already created by the setup tool):
  - Tags: `Player` (built in), `Enemy`, `WeakPoint`, `Interactable`, `Ground`
  - Layers: `Player`, `Enemy`, `Interactable`, `Ground`, `Water` (built in)

### B
- [x] Install ProBuilder (6.1.4, in `Packages/manifest.json`)
- [x] Run the Phase 0 setup tool (runs automatically once; also at **Tools > Signal Lost > Run Phase 0 Setup**). It did the next four items:
  - [x] Create the folder structure: `Assets/_Project/{Scripts, Scenes, Prefabs, Materials, Art, Audio, UI}`
  - [x] Create the scenes `MainMenu`, `IslandExterior`, `UndergroundFacility` and add them to Build Settings in that order
  - [x] Add the tags and layers
  - [x] Delete `SampleScene`, `TutorialInfo`, and `Readme.asset`
- [x] Draft the shared code for Partner A to review: `IInteractable`, `IDamageable` + `DamageType`, `GameManager` (in `Assets/_Project/Scripts/Core`)

### A
- [x] Create a private test scene `Sandbox_A` (only A edits it) – floor, wall, HUD, Player, 3 test dummies with visor weak points
- [x] Set up input: Move, Look, Jump, Sprint, Fire, AltFire, Reload, Interact, Pause (defined in code in `PlayerInputReader.cs`, keyboard/mouse + gamepad)

**Done when:** both partners have the project open on the same commit, the three scenes are in Build Settings, and the interfaces are committed.

---

## Phase 1 – Foundation (Day 1)

Goal: walk from the safe room to the bunker and back, and shoot a test target.

### A – Player & weapon basics
- [x] `FirstPersonController.cs`: walk, sprint with stamina, jump, mouse look, gravity
- [x] `PlayerHealth.cs` implementing `IDamageable` (HP, death event)
- [x] `PlayerInteraction.cs`: 3 m raycast, finds `IInteractable`, shows the prompt text, presses E
- [x] `ArcPistol.cs`: Pulse fire (projectile), 12-round magazine, reload with R
- [x] `Projectile.cs` and a test dummy with `Health.cs` (+ `WeakPoint.cs` for 45-damage crits, `TestDummy.cs`)
- [x] Player prefab saved in `Prefabs/Player` (built by the `player` bridge command; `PlayerPersistence.cs` carries HP/ammo between scenes)

### B – World skeleton & core systems
- [x] `GameManager.cs`: singleton that survives scene loads; stores fuses (0–3), purge timer (20:00), player HP/ammo, cleared encounter IDs
- [x] Purge timer counts down and fires a "time up" event
- [x] `SceneLoader.cs`: loads a scene with a fade to black and back
- [x] `SpawnPoint.cs`: places the player at the right spot after a scene loads (safe room, elevator top, elevator bottom)
- [x] `ScenePortal.cs`: elevator interactable that calls the scene loader
- [x] **Island greybox** (`IslandExterior`):
  - [x] Terrain 250×250 m with water around it
  - [x] Safe room and armory (greybox boxes)
  - [x] Dirt road route, elevator house, hill trail, tower compound, pier
  - [x] Invisible walls or cliffs at the edges of the play area
- [x] **Bunker greybox** (`UndergroundFacility`): elevator room, central hub, Wing A corridor and breaker room, Wing B flooded generator and quarantine room
- [x] Bake the NavMesh in both scenes (NavMesh Surface component)
- [x] HUD skeleton: HP bar, stamina bar, timer, objective text, fuse counter, ammo text, center prompt (placeholder values are fine)

> **Tools for B (all under Tools > Signal Lost):**
> * **Build HUD Prefab** – creates `Assets/_Project/UI/HUD.prefab` wired to `HUDController`.
> * **Build Greybox (Both Scenes)** – builds the island and bunker blockouts, spawn points, elevator panels, the HUD, a temporary `DebugPlayer`, and bakes the NavMesh. Re-running rebuilds the `Greybox` object, so move hand-made changes out of it first.
> * TextMeshPro resources are imported (Assets/TextMesh Pro).
> * If the Tools menu items don't appear (seen in Unity 6.4), create an empty file `Library/SignalLost/Commands/<command>.cmd` (`setup`, `import-tmp`, `hud`, `greybox`, `player`, `swap-player`, `sandbox`, `phase2`, `phase3`, `smoketest`) and the Editor runs it within a second. Results: `Library/SignalLost/bridge.log`.
> * `DebugPlayer` (WASD, mouse, Space, E) is only for testing until A's Player prefab arrives. Delete it then.
> * Elevator requirements are set to None for now; Phase 2 sets them to Arc Pistol / Fuses 1 and 2.

### Both – end of day
- [x] A delivers the Player prefab; B places it in both scenes (DebugPlayer removed)
- [ ] Merge both branches into `main`, then play-test together for 10 minutes

**Done when:** you can start in the safe room, walk to the elevator, ride it to the bunker, ride back, and arrive at the elevator (not the safe room), with the timer still counting down and HP/ammo kept. You can shoot the test dummy.

> ✅ Passed 2026-10-30: elevator round trip, correct arrival point, timer keeps running, ammo carries over, Sandbox_A dummies take 20 body / 45 visor damage. **Phase 1 complete.**

---

## Phase 2 – Combat & Interactions (Day 2)

Goal: Phases 1 and 2 of the game (island road and bunker) are playable in greybox.

### A – Enemies & weapon
- [x] `EnemyAI.cs` state machine: Patrol → Chase → Attack, plus Stunned
- [x] Vision cone (15 m, 90°) with line-of-sight raycast; hearing gunfire within 15 m
- [x] Squad alert: an alerted bot alerts every bot within 15 m
- [x] Sensor light color changes with state (blue / yellow / red, flicker when stunned)
- [x] `EnemyStats` ScriptableObject (HP, speed, range, damage, fire rate)
- [x] `WeakPoint.cs`: crit multiplier on the eye/visor collider
- [x] **Enforcer Unit**: greybox robot (capsule body, glowing visor weak point, arm cannon). EnemyAI already drives Animator parameters `Speed`, `Shoot`, `Hit`, `Die`.
  - [ ] Swap in Robot Kyle + Mixamo animations (needs your Asset Store and Adobe accounts; done in the Phase 4 art pass)
- [x] **Recon Stinger**: greybox hovering drone, zig-zag approach, shock beam
  - [ ] Swap in the Buster Drone model (Phase 4 art pass)
- [x] Enemy laser projectiles (visible, 25 m/s) with a 0.4 s telegraph flash
- [x] Overload Beam: hold to charge 1 s, hitscan, 4 ammo, 2 s cooldown
- [x] `ExplosiveBarrel.cs` (75 damage, 5 m radius, hurts the player too) and `Destructible.cs` crates with drop table

### B – Interactions & bunker challenges
- [x] `Door.cs` + keypad: opens a sliding door; locked doors show the reason
- [x] `Pickup.cs`: Battery Clip (+12 ammo), Medkit (+35 HP), Override Fuse
- [x] NPC dialogue: Dr. Reyes briefing panel (3–4 lines), closing it unlocks the armory
- [x] Elevator rules: going down needs the Arc Pistol; going up needs Fuses 1 and 2
- [x] **Wing A:**
  - [x] `LaserTripwire.cs`: beams deal 25 damage and trigger the alarm; shooting the emitter disables them
  - [x] Maintenance log terminal showing the breaker order
  - [x] `BreakerPuzzle.cs`: random order each run; wrong order resets the levers and calls an Enforcer; right order opens the Fuse 1 pod
- [x] **Wing B:**
  - [x] `ElectrifiedWater.cs`: 10 HP per second, HUD hazard warning
  - [x] Crate stepping stones (≤ 2 m apart) to the valve
  - [x] Drainage valve: drains the water and opens the path
  - [x] `LockdownRoom.cs`: picking up Fuse 2 seals the doors, starts a 45 s countdown, spawns 2 Enforcers + 1 Recon Stinger (the Aegis Warden replaces the Stinger in Phase 3); doors open when all are destroyed or time runs out
- [x] HUD shows interaction prompts and the fuse counter updates

### Both – end of day
- [x] B places A's enemy prefabs on the island road and in the bunker
- [ ] Merge into `main` and play-test

> Built by the `phase2` bridge command (`Phase2ContentBuilder.cs`): prefabs in `Prefabs/Enemies`, `Pickups`, `Props`; scene content under the `Gameplay` object. Re-running rebuilds `Gameplay` only. `TemporaryDeathRestart` reloads the scene on death until Phase 3's Game Over screen.

**Done when:** you can play from the safe room through both bunker wings, collect Fuses 1 and 2, and return to the surface, fighting enemies along the way.

> ✅ Passed 2026-10-30 (greybox enemies). **Phase 2 complete.**

---

## Phase 3 – Full Game Loop (Day 3)

Goal: the game can be played from the main menu to the Victory screen (the boss can be Phase 1 only for now).

### A – Heavy enemies & encounters
- [x] **Aegis Warden**: bigger Kyle variant with a front shield; shield blocks Pulse, breaks for 6 s on Overload or explosion
- [x] `WaveSpawner.cs`: 90-second upload, 3 escalating waves from the spawn gates
- [x] `BossController.cs` Phase 1: riot shield, plasma orbs (shootable), shoulder vents take damage while the shield is down
- [x] Boss health bar hooked to the HUD

### B – Objectives, flow & menus
- [x] `ObjectiveManager.cs`: ordered objective list; updates the HUD objective text
- [x] Radio messages from Dr. Reyes at each objective (subtitle box, bottom-left)
- [x] Fuse console at the radio tower: insert fuses one at a time; all 3 starts the upload
- [x] Signal upload progress bar on the HUD
- [x] Return trip: Phase 1 enemies stay dead; reinforcement patrol on the hill trail
- [x] Service road gate to the pier opens after the upload
- [x] Extraction boat interaction → Victory screen
- [x] **Checkpoints** CP1–CP4: save a snapshot; "Retry from Checkpoint" restores it exactly
- [x] **Menus:**
  - [x] Main Menu: Play, Controls, Settings, Credits, Quit
  - [x] Pause Menu (Esc): stops time, unlocks the cursor, pauses audio; Resume, Retry from Checkpoint, Settings, Main Menu
  - [x] Game Over: cause of failure; Retry from Checkpoint, Restart, Main Menu
  - [x] Victory: completion time, time left, enemies destroyed, accuracy
  - [x] Settings: mouse sensitivity, master / music / SFX volume

### Both – end of day
- [ ] Merge and play the whole game once, start to finish, timing the run

> Built by the `phase3` bridge command (`Phase3ContentBuilder.cs`): Aegis Warden, Titan and orb prefabs, `GameMenus` prefab, the `MainMenu` scene, and the game loop under `Gameplay_Phase3` in both scenes. Checkpoints: CP1 (bunker arrival), CP2 (hub), CP3 (surface return with Fuses 1+2), CP4 (pier, after the upload). The upload only progresses while the player is inside the compound.

**Done when:** a full run works from Main Menu to Victory; dying shows Game Over with a working checkpoint retry; the timer running out also shows Game Over.

---

## Change Request – Armory Code Puzzle (added 2026-10-30)

Dr. Reyes sends the player to a new maintenance bunker; a randomized code puzzle there opens the armory keypad.

- [x] `GameManager.ArmoryPuzzle`: random digits, color order, hidden locker, and decoys per run
- [x] `ArmoryCodePuzzle.cs`: wall message, locker digits, decoys, glow digit
- [x] `LightSwitch.cs`: lights off reveals the hidden glow digit and dims the painted ones
- [x] `CodeKeypad.cs` + HUD code panel: type 4 digits, ACCESS DENIED / ACCESS GRANTED, Esc cancels without pausing
- [x] New briefing lines, objectives, radio line on entering the bunker
- [x] Maintenance bunker ~100 m northwest of the safe room (placed by hand; `CodeBunkerCenter` in the builder matches, and **Tools > Signal Lost > Repaint Bunker Path** / the `bunker-path` command repaints the path after moving it again); first road Enforcer moved >15 m from the safe room door
- [x] Play-test: briefing → bunker → solve → keypad → Arc Pistol (passed 2026-10-30)

---

## Change Request – Elevator Power (added 2026-10-30)

The freight elevator starts unpowered; a guarded generator bunker east of the road holds the main power lever.

- [x] `PowerSwitch.cs`: main power lever (persists for the run), spark + "POWER RESTORED", work light on
- [x] `ScenePortal.cs`: `requiresPower` + red/green status light; prompt "No power – restore it at the generator bunker"
- [x] Generator bunker at X 175, Z 75 with a dirt path from the road; guarded by **2 Enforcers + 2 Recon Stingers** and a red barrel; medkit and battery inside
- [x] Radio lines: "no power" at the elevator (only before the switch), "power's back" after it
- [x] Objective: "Restore power at the generator bunker east of the road" between the pistol and the elevator
- [x] Play-test: pistol → generator bunker fight → lever → elevator down (passed 2026-10-30)

---

## Change Request – Underground Puzzles (added 2026-10-30)

Three more puzzles in the underground facility (options 1D, 3 and 4).

- [x] **Gauge lock (Wing A):** three random pressure gauges between the laser sets; breaker room door with a 3-digit keypad (`Gauge.cs`, `CodeKeypad` gauge mode)
- [x] **Pipe routing (Wing B):** pipe schematic + junction levers A/B/C + PUMP replace the drainage valve (`PipeRouting.cs`, `PipeLever.cs`, `PumpHandle.cs`); solution A=DOWN, B=UP, C=DOWN
- [x] **Junction box (hub):** random solvable 3×3 wire grid; the elevator up needs both fuses **and** the restored circuit (`WiringPuzzle.cs`, `WireTile.cs`)
- [x] Objectives, radio lines, and elevator status light updated; Game Concept updated (Puzzle Overview, objective chain, sections 4 and 10, rubric)
- [x] Automated test: gauge code opens the breaker room; wrong routing doesn't drain, correct does; elevator up locked until the junction box is solved
- [x] Play-test the three puzzles by hand (all working)
- [x] Pipe routing made clearer after play-test: lit route preview, UP/DOWN labels on the schematic, lever state labels, failure message names the junction
- [x] Re-test the clearer pipe routing (clear now)
- [ ] Time a full run, then re-tune the purge timer (currently 20:00)

---

## Phase 4 – Boss, Art & Audio (Day 4)

Goal: content complete. **Feature freeze at the end of this phase.**

### A – Boss & combat feel
- [ ] Boss Phase 2: pylon tethers, invulnerability, shockwave stomp, stun window, pylons reboot
- [ ] Import the Titan model (Yellow Heavy Robot); rig with Mixamo or animate in code
- [ ] Hit feedback: hit markers, crit marker, enemy hit flash, sparks, screen shake on Overload
- [ ] `LightningLine.cs` for the Overload Beam and tethers
- [ ] Balance pass on damage, HP, and fire rates (see the tables in GameConcept.md)

### B – Art pass
- [ ] Download the approved assets (see the asset list in GameConcept.md, section 13) at 2K; set texture Max Size to 2048
- [ ] Add every asset to `Credits.txt` as it is imported
- [ ] Island: paint terrain layers, place pine trees and rocks, add the ocean water
- [ ] Sky: Poly Haven sunset HDRI as the skybox; match the sun color and angle
- [ ] Post-processing Volume: ACES tonemapping, bloom, color adjustments, vignette; SSAO on the URP renderer
- [ ] Bunker: apply concrete and metal textures to the ProBuilder geometry; add props, lights, and red/cyan hazard lighting
- [ ] Replace placeholder crates, barrels, and medkits with Poly Haven models
- [ ] UI: apply the Kenney Sci-Fi UI graphics and the Rajdhani font
- [ ] Re-bake the NavMesh after placing props

### B – Audio pass
- [ ] Audio Mixer with Music and SFX groups (connected to the Settings sliders)
- [ ] `MusicManager.cs`: exploration, combat, and boss tracks with crossfades
- [ ] Footsteps by surface (dirt, metal)
- [ ] SFX: weapon, reload, hits, explosions, crates, lasers, water crackle, siren, radio static, UI clicks
- [ ] 3D audio (`spatialBlend = 1`): fuse and pylon hums, robot servos, alert beeps, boss footsteps

### Both – end of day
- [ ] Merge and play a full run
- [ ] **Feature freeze:** from now on, only bug fixes and tuning

**Done when:** every feature in GameConcept.md is in the game (or cut on purpose), with final art and sound.

---

## Phase 5 – Polish & Ship (Day 5)

Goal: a stable Windows build and everything needed for grading.

### Both
- [ ] Each partner plays at least 2 full runs; write every bug in a shared list
- [ ] Fix bugs, most serious first (crashes, soft-locks, progression blockers)
- [ ] Time the runs: a first-time run should take 12–15 minutes; tune if not
- [ ] Check every lose condition (0 HP, timer) and every checkpoint retry
- [ ] Check the game can't get stuck: no ammo dead-ends, no doors locked forever, no falling out of the map
- [ ] Credits screen lists every asset and license
- [ ] Build for Windows (File > Build Profiles); test the .exe on a different PC
- [ ] Record a full gameplay video as a backup for the presentation
- [ ] Update GameConcept.md so it matches the final game (names and the student details at the top)
- [ ] Prepare the presentation: rubric table, key features, short demo route

**Done when:** the build runs on another PC from Main Menu to Victory without errors, and the video and document are ready to submit.

---

## Cut List (use only if a phase falls behind)

Cut in this order. Nothing in the grading rubric is ever cut.

1. Boss Phase 2 shockwave stomp
2. Randomized breaker order (use a fixed order)
3. Wing B crate jumping (use a raised catwalk to the valve)
4. Pylon reboot cycle (pylons stay destroyed after one stun)
5. Crate break-apart effect (crate disappears and drops its item)
6. Accuracy stat on the Victory screen
