# Signal Lost – Development Roadmap

The full design is in [GameConcept.md](GameConcept.md). This file breaks the build into phases. Each phase ends with a **Done when** test: don't start the next phase until it passes.

**How to use this file**
* Tick a box by changing `- [ ]` to `- [x]` and committing. GitHub shows the boxes as checkboxes.
* **A** = Partner A (gameplay: player, weapon, enemies, boss). **B** = Partner B (world & systems: scenes, GameManager, interactions, puzzles, UI, audio). **Both** = do it together.
* Day numbers are targets. If a phase runs late, use the cut list at the bottom instead of skipping the Done-when test.

## Current Status (updated 2026-10-31)

| Phase | Status |
| :---- | :---- |
| 0 – Setup | ✅ Done (single PC; GitHub push still to do) |
| 1 – Foundation | ✅ Done, play-tested |
| 2 – Combat & Interactions | ✅ Done, play-tested |
| 3 – Full Game Loop | 🟡 Built; all parts tested — **one full timed run from Main Menu to Victory still needed** |
| Change requests (armory code, elevator power, underground puzzles) | ✅ Done, play-tested |
| 4 – Boss, Art & Audio | 🟡 Code done (Titan Phase 2, hit feedback, lightning, post-processing); art and audio passes waiting on asset downloads |
| 5 – Polish & Ship | 🟡 Started: Windows build made and launches, credits screen done, fall recovery added; needs your play-testing, a different-PC test and the video |

**Git:** all work is committed locally on branch `docs/roadmap` (latest: `3852fab`). `main` holds only the template commit. Nothing is pushed to GitHub yet.
**Note:** Partner A's tasks were built in this same project, so "merge" items mean merging `docs/roadmap` into `main`.

---

## Phase 0 – Setup (Day 1 morning, ~2 hours)

Goal: both partners can open the same project, and the shared code contracts are agreed before anyone writes gameplay code.

### Both
- [x] Install Unity 6000.4.0f1 (this PC; install the same version on any second PC)
- [x] Make the first commit on `main` (`ba12db0`)
- [ ] Push to GitHub (`git push -u origin docs/roadmap` and `git push origin main`)
- [ ] Partner A clones the repo and opens the project without errors (only needed if a second PC is used)
- [x] Git rules: work on a branch (`docs/roadmap`), commit after each tested step, never edit the same scene from two PCs at once
- [x] Read through GameConcept.md and agree on changes (reviewed and extended several times)
- [x] Agree and commit the shared interfaces before splitting up (committed in `f193a8e`):
  - [x] `IInteractable` (Prompt, CanInteract, Interact)
  - [x] `IDamageable` (TakeDamage with amount, DamageType, hit point)
  - [x] `DamageType` enum (Pulse, Overload, Explosion, Hazard)
  - [x] `GameManager` public API (player HP/ammo save and load, enemy-destroyed events, fuses, timer, checkpoints)
- [x] Agree layers and tags (created by the setup tool):
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

> ✅ Passed on this PC (scenes in Build Settings, interfaces committed). A second PC only needs to clone after the GitHub push.

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
> * If the Tools menu items don't appear (seen in Unity 6.4), create an empty file `Library/SignalLost/Commands/<command>.cmd` (`setup`, `import-tmp`, `hud`, `greybox`, `player`, `swap-player`, `sandbox`, `phase2`, `phase3`, `postfx`, `bunker-path`, `art-island`, `art-bunker`, `art-props`, `art-enemies`, `art-ui`, `audio`, `refresh`, `smoketest`) and the Editor runs it within a second. Results: `Library/SignalLost/bridge.log`.
> * `DebugPlayer` (WASD, mouse, Space, E) is only for testing until A's Player prefab arrives. Delete it then.
> * `smoketest` plays the game automatically (Main Menu → briefing → both island bunkers → underground puzzles → Titan Phase 2) and writes results to `bridge.log`.

### Both – end of day
- [x] A delivers the Player prefab; B places it in both scenes (DebugPlayer removed)
- [ ] Merge `docs/roadmap` into `main` (work is committed on `docs/roadmap`)
- [x] Play-test for 10 minutes

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
  - [x] Swap in the Sci-Fi Robots Bundle "Robert" (replaces Robot Kyle; Engie for the Aegis Warden) + Mixamo animations (Humanoid; idle/walk/run blend, firing, hit reaction, death; visor weak point and muzzle ride the head and hand bones) – `EnemyArtBuilder.cs`, `art-enemies` command
- [x] **Recon Stinger**: greybox hovering drone, zig-zag approach, shock beam
  - [x] Swap in the Buster Drone model (its own hover animation, eye weak point on the front)
- [x] Enemy laser projectiles (visible, 25 m/s) with a 0.4 s telegraph flash
- [x] Overload Beam: hold to charge 1 s, hitscan, 4 ammo, 2 s cooldown
- [x] `ExplosiveBarrel.cs` (75 damage, 5 m radius, hurts the player too) and `Destructible.cs` crates with drop table

### B – Interactions & bunker challenges
- [x] `Door.cs` + keypad: opens a sliding door; locked doors show the reason
- [x] `Pickup.cs`: Battery Clip (+12 ammo), Medkit (+35 HP), Override Fuse
- [x] NPC dialogue: Dr. Reyes briefing panel *(now 5 lines and sends the player to the armory code puzzle – see Change Request)*
- [x] Elevator rules: going down needs the Arc Pistol; going up needs Fuses 1 and 2 *(now also main power down and the junction box up – see Change Requests)*
- [x] **Wing A:**
  - [x] `LaserTripwire.cs`: beams deal 25 damage and trigger the alarm; shooting the emitter disables them
  - [x] Maintenance log terminal showing the breaker order
  - [x] `BreakerPuzzle.cs`: random order each run; wrong order resets the levers and calls an Enforcer; right order opens the Fuse 1 pod
- [x] **Wing B:**
  - [x] `ElectrifiedWater.cs`: 10 HP per second, HUD hazard warning
  - [x] Crate stepping stones (≤ 2 m apart) to the drain controls
  - [x] Drainage valve: drains the water and opens the path *(replaced by the pipe routing puzzle – see Change Request)*
  - [x] `LockdownRoom.cs`: picking up Fuse 2 seals the doors, starts a 45 s countdown, spawns 2 Enforcers + 1 Recon Stinger (the Aegis Warden replaces the Stinger in Phase 3); doors open when all are destroyed or time runs out
- [x] HUD shows interaction prompts and the fuse counter updates

### Both – end of day
- [x] B places A's enemy prefabs on the island road and in the bunker
- [ ] Merge into `main`
- [x] Play-test

> Built by the `phase2` bridge command (`Phase2ContentBuilder.cs`): prefabs in `Prefabs/Enemies`, `Pickups`, `Props`; scene content under the `Gameplay` object. Re-running rebuilds `Gameplay` only. (The temporary death restart was replaced by Phase 3's Game Over screen.)

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
- [ ] Merge into `main`
- [ ] Play the whole game once, start to finish, timing the run (also check: die once → Retry from Checkpoint; Titan fight; leaving the compound pauses the upload)

> Built by the `phase3` bridge command (`Phase3ContentBuilder.cs`): Aegis Warden, Titan and orb prefabs, `GameMenus` prefab, the `MainMenu` scene, and the game loop under `Gameplay_Phase3` in both scenes. Checkpoints: CP1 (bunker arrival), CP2 (hub), CP3 (surface return with Fuses 1+2), CP4 (pier, after the upload). The upload only progresses while the player is inside the compound.

**Done when:** a full run works from Main Menu to Victory; dying shows Game Over with a working checkpoint retry; the timer running out also shows Game Over.

> 🟡 Main Menu → Play fixed and verified (HUD was missing parts). Every section has been play-tested on its own; the single full timed run is the last check.

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
- [x] `ScenePortal.cs`: progress lock (`requiredClearedId`) + red/green status light; prompt "No power – restore it at the generator bunker"
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
- [ ] Time a full run (Phase 3 check). Decision: the purge timer **stays at 20:00**

---

## Phase 4 – Boss, Art & Audio (Day 4)

Goal: content complete. **Feature freeze at the end of this phase.**

### A – Boss & combat feel
- [x] Boss Phase 2: pylon tethers, invulnerability, shockwave stomp, stun window, pylons reboot (+ 2 Stinger reinforcements, chest core double damage) – `BossPylon.cs`, `BossCore.cs`, `ShockwaveRing.cs`
- [x] Import the Titan model (Yellow Heavy Robot); animated in code (`MechWalkBob.cs`: walking bob/sway, idle breathing); greybox vents, chest core and riot shield stay as the readable weak points. Extraction boat swapped for the Small Motorboat
- [x] Hit feedback: hit markers (red X for crits), enemy hit flash, sparks, screen shake on Overload and damage, red damage flash – `ImpactEffects.cs`, `CameraShake.cs`
- [x] `LightningLine.cs` for the Overload Beam and tethers
- [x] Death explosions: Particle Pack SmallExplosion on Enforcers, Aegis Wardens and Stingers, BigExplosion on the Titan (`DeathExplosion.cs`)
- [x] Shields as see-through capsule bubbles around the whole Aegis Warden and Titan (was a flat front box; same rules: blocks Pulse, Overload or explosions drop it for 6 s, so flanking no longer gets past it)
- [x] Fix: Enforcer walk/run stutter (Mixamo root motion was baked into the pose, so clips not downloaded "In Place" drifted and snapped back each loop)
- [x] Enforcer scaled 1.5x (about 2.8 m; hitbox, visor, muzzle, agent and death explosion scale with it – `EnforcerScale` in `EnemyArtBuilder.cs`). It is now taller than the Aegis Warden, on purpose
- [x] Balance review (2026-10-31): prefab values match the GameConcept tables except the Recon Stinger shock beam (6 dmg/s in code, table said 12) and the Titan plasma orbs (10 dmg in code, table said 15). The tested code values were kept and the tables corrected. Further tuning waits for the full timed run (purge timer stays 20:00)

### B – Art pass
- [x] Download Part A of `Docs/ASSET_DOWNLOADS.md` (Poly Haven + OpenGameArt, 164 MB) into `Assets/_Project/Art/PolyHaven` and `Assets/_Project/Audio`; imported without errors
- [x] You: download Part B (Asset Store, Mixamo, Sketchfab) – see `Docs/ASSET_DOWNLOADS.md`. Sci-Fi Robots Bundle used instead of Robot Kyle
- [x] Clean up Part B: demo scenes, unused robots, other render-pipeline packages, unused particle and UI files moved to `_RemovedAssets/` (git-ignored backup, 237 MB)
- [ ] Pick 2 music tracks (calm + intense) from the Sci-Fi Music Loops Pack; move the other 9 out (~330 MB)
- [x] Shrink the Titan's 8K textures to 2K (572 MB → 23 MB; metallic + roughness packed into a URP metallic/smoothness map; 8K originals in `_RemovedAssets/`)
- [x] `Assets/_Project/Credits.txt` created with every downloaded asset (Part B entries listed, to confirm on import)
- [x] Island: paint terrain layers (grass, dirt paths, rock on slopes/hill, sand on the shore), 480 conifers (doubled 2026-10-31) with solid trunk colliders (own prefab variants in `Art/Island/Trees`) kept off roads, paths, buildings and patrol routes, glossy ocean material – `IslandArtBuilder.cs`, `art-island` command
- [x] Island: 240 Poly Haven boulders and mossy rocks (tripled from 77 on 2026-10-31) along the shore, beside the hill trail and on open ground, laid on their flattest side, kept off paths and away from trees and buildings – `PropArtBuilder.cs`, `art-props` command
- [x] Island: 7 rain puddles on flat stretches of the dirt roads and paths (glossy, sky-reflecting, no collider) – `PropArtBuilder.cs`, `art-props` command
- [x] Sky: Belfast sunset HDRI skybox, rotated so the sun sits NNE behind the radio tower; sun, ambient (trilight) and fog colors sampled from the sky
- [x] Post-processing Volume: ACES tonemapping, bloom, color adjustments, vignette (outdoor + darker bunker profile; `postfx` command)
- [x] SSAO: the Screen Space Ambient Occlusion feature was already on `Assets/Settings/PC_Renderer` (template default, depth-normals); raised Intensity 0.4 → 0.8 and Radius 0.3 → 0.4
- [x] Bunker: concrete and metal textures on the greybox geometry (world-space triplanar shader `SignalLost/Triplanar Lit`, so stretched cubes don't smear; also reaches the island buildings and both island bunkers), military crates in room corners, brighter room lamps, red/cyan hazard lighting with ceiling light strips, flickering water glow and a pulsing quarantine alarm (`LightFlicker.cs`) – `BunkerArtBuilder.cs`, `art-bunker` command
- [x] Replace placeholder crates, barrels, and medkits with Poly Haven models (models fitted inside the greybox shapes, which keep their colliders and scripts; `Phase2ContentBuilder` re-applies them when it rebuilds the prefabs)
- [x] UI: Kenney Fantasy UI Borders (CC0) + Rajdhani font on the HUD, pause/game over/victory/settings menus and the Main Menu – framed charcoal panels, line-frame buttons that turn amber on hover, fading amber dividers under titles and beside the timer, thin frames on the bars, off-white text with amber accents (cyan removed) – `UIArtBuilder.cs`, `art-ui` command (also re-applied by the HUD and Phase 3 builders). Kenney Sci-Fi UI moved to `_RemovedAssets/` (unused)
- [x] Maintenance bunker rework (2026-10-31, `DetailArtBuilder.CodeBunker.cs`; also run by `phase2` after it rebuilds the bunker): the four colored lockers are gone. The code digits are painted smudges on the north and south walls in the four code colors (blue, red, green, yellow), the hidden digit is a scratched "#" with a glow-paint twin that shows only with the lights off, and 10 decoy smudges (white, orange, purple, black, brown, so never a code color) carry random 1–4 digit numbers each run, plus 4 blank streaks. Every number has hand-painted variance (italic slant, per-character tilt, drift and spacing, mixed fonts and sizes); the wall message is slanted red marker text. The code is still 4 digits read in the wall message's color order. The light switch is a steel surface-mounted toggle with cover plate, screws, ON/OFF labels, a chrome stem with a rubber tip and a conduit; the toggle flips up and down (`LightSwitch` got `leverAxis`, `onAngle`, `offAngle`).
- [x] Detail art pass (2026-10-31) – `DetailArtBuilder.cs` (+ partial files), `art-details` command, everything under an `ArtPass_Details` object per scene; greybox shapes keep colliders and scripts and only lose their renderer; safe to re-run:
  - Sci-fi Pistol [PBR] on the player (URP material, cyan emission) and as the Arc Pistol pickup; Sketchfab battery cell and cartridge fuse on the Battery Clip and Override Fuse pickups
  - Main power switch: Poly Haven `utility_box_01` cabinet with a knife-switch lever; fuse console as an industrial terminal (bezels, screens) with a generator, cable and a field radio (`vintage_radio_transceiver`) beside it; military ammo lockers; fuse pedestals with steel plinths and a glowing cap; junction box frame and conduits
  - Freight elevator (both scenes): steel cage with yellow frame, lattice sides, folded scissor gate, hazard threshold, caged roof lamp, hoist motor and ropes, guide rails, and a real call panel
  - Radio tower: red and white lattice tower with platforms, a parabolic dish, panel antennas, pulsing red beacons, guy wires and a concrete footing
  - Island buildings (safe room, elevator house, both bunkers): plinth, pilasters, roof cap with parapet, roof vents and stack, canopy with a caged lamp, yellow door frame, apron, wall cabinet, name plate; generator set inside the generator bunker
  - Chain-link fence (Poly Haven `modular_chainlink_fence`) around the tower compound: 2 m panels and posts that follow the hill, plus a chain-link leaf on the sliding service gate; the old wall slabs keep their colliders (`DetailArtBuilder.Fence.cs`)
  - Underground rooms: ceiling beams, corner pillars, pipe runs with valves, caged lamps, door frames, shelves, power cabinets and a generator (spots occupied by gameplay objects are skipped)
  - Maintenance bunker furniture (`DetailArtBuilder.CodeBunkerFurniture.cs`): two steel shelves on the east wall stocked with jerrycans and military crates, a workbench (toolbox, vise, papers, mug, jerrycan), a desk with the field radio on the west wall, a stool, and crate stacks replacing the plain supply box; everything keeps the doorway, switch and wall clues clear and has colliders
  - Generator bunker interior (`DetailArtBuilder.GeneratorBunker.cs`): ceiling beams with caged lamps and pipe runs, two stocked steel shelves, a workbench with tools, jerrycans, oil stains and a hazard border around the generator, fire extinguisher, high-voltage plate, and cables to the power cabinet; furniture now sits on the true floor top in both bunkers
  - Wing A laser corridor rebuilt (`DetailArtBuilder.Lasers.cs`, `lasers` command; also called by `phase2`): six fixed gates (never randomized) with the gap alternating north and south so the player weaves left and right; beams are straight, slanted across the corridor, tilted in height, or both; emitters are steel columns with lenses, receiver posts have ports, beams have a bright core and a soft halo, floor marks show the blocked side. Verified with a capsule route search that a path exists from the hub to the breaker door
  - Ammo locker and medkits outside the generator bunker and at the pier; reserve ammo cap is 72 (`ArcPistol.MaxReserve`); Game Concept updated from 48
  - Pier: plank deck, fascia and piles, railings on the open edges, bollards and rope by the boat, life ring, lamp posts, corrugated painted containers, a harbour shed, crates, jerrycans and a net pile (`DetailArtBuilder.Pier.cs`)
  - Central Hub lighting (`DetailArtBuilder.HubLighting.cs`): the single blown-out ceiling light is now a soft fill; each caged lamp has its own warm light with soft shadows from the beams and pillars; red accent over the laser wing door, cyan over the flooded wing door, a cyan wash on the junction box, and low cool corner fills
- [x] Water pass (2026-10-31) – `SignalLostWater.shader` (`SignalLost/Water`), `WaterArtBuilder.cs`, `water` command (also applied by `art-island` and `phase2`): the island sea and the Wing B floodwater use one custom URP shader with animated ripples that calm with distance, depth-based color with refraction of the seabed (extra color depth so the sea reads deep), sunset sky reflection with fresnel and sun glint, and foam along shores and around the stepping crates. The island sea is teal near the shore and dark blue offshore. Wing B is dark murky water with thin, patchy, flickering cyan electric arcs, lamp highlights and a foam rim. Needs Depth Texture and Opaque Texture on the URP asset (both on in `PC_RPAsset`). Materials in `Art/Water`. Drain animation and damage are unchanged.
- [x] NavMesh after props: rocks and military crates carve the NavMesh at runtime (NavMeshObstacle), so no re-bake is needed

### B – Audio pass
- [x] Audio Mixer `Audio/MainMixer` (Master > Music, SFX; `MusicVolume` / `SfxVolume` exposed) connected to the Settings sliders (`SettingsPanel` → `Sfx.SetMixerVolume`; master still uses `AudioListener.volume`)
- [x] `MusicManager.cs`: exploration / combat (any bot in Chase or Attack, held 5 s) / boss (player within 70 m of a living Titan) with crossfades. **Stand-in tracks** (`loop_ambient_01`, `loop_machine_02`, `boss_battle_loop.mp3`) until the two Music Loops Pack tracks are picked; set them on `Resources/AudioLibrary` (Exploration / Combat)
- [x] Footsteps by surface (`FootstepPlayer.cs`: gravel on terrain, stone on concrete and metal; landing thud)
- [x] SFX: weapon, overload charge, reload, dry fire, hits and crits, player hurt, explosions, crates, lasers, water crackle, siren, radio static, UI clicks, keypad, doors, pickups, shield break
- [x] 3D audio (`spatialBlend = 1`): fuse and pylon hums, robot servos (`EnemyAudio.cs`), alert beeps, shock beam, laser hum, Titan footsteps and stomp charge-up
- [x] `Sfx.cs` (sound by name: `Sfx.Play`, `Play2D`, `Loop`), `AudioLibrary` asset in `Resources` (swap any clip in the Inspector), `AudioBuilder.cs` (`audio` bridge command; re-running only adds missing ids), 9 synthesized sounds in `Audio/Generated`
- [ ] Listen through and swap any clip that doesn't fit (clip choices were made from file names)
- [x] Music picked (2026-10-31) by loudness/brightness analysis, since the tracks were not auditioned: calm = `08. Cut Scene` (quietest, darkest) → `Audio/Music/music_exploration_cut_scene.wav`; intense = `02. Space Battle` (loud, bright) → `music_combat_space_battle.wav`; both import as streaming Vorbis. The other 9 are in `_RemovedAssets/SciFiMusic_unused/` (git-ignored). `AudioLibrary` got per-track gains (exploration 1.0, combat 0.35 because that loop is mastered ~6× louder, boss 0.8) used by `MusicManager`. **Swap them in `AudioLibrary` if you dislike them.**

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
- [ ] Time the runs: a first-time run should finish with a few minutes left on the purge timer; tune if not
- [ ] Check every lose condition (0 HP, timer) and every checkpoint retry
- [ ] Check the game can't get stuck: no ammo dead-ends, no doors locked forever, no falling out of the map *(partly done: ammo lockers at the hub, compound, generator bunker and pier; `FallRecovery.cs` returns the player to the last safe ground if they fall below y = -25. Doors and the full path still need a human run)*
- [x] Credits screen lists every asset and license: scrollable panel filled from `Resources/Credits.txt` (`CreditsText.cs`, `CreditsScreenBuilder.cs`, `credits` command, also used when `phase3` rebuilds the menu). **Still to fill in:** team names and the Sketchfab authors/licenses for the battery and fuse models
- [x] Build for Windows (`build` bridge command, `BuildTools.cs`): `Builds/SignalLost/ProjectOutpost_SignalLost.exe`, 595 MB, 0 errors, launched for 40 s with no exceptions (2026-10-31). Delete the `*_BurstDebugInformation_DoNotShip` folder before zipping
- [ ] Test the .exe on a different PC
- [ ] Record a full gameplay video as a backup for the presentation
- [ ] Update GameConcept.md so it matches the final game (names and the student details at the top)
- [x] Presentation notes drafted: [PRESENTATION.md](PRESENTATION.md) (pitch, demo route, points per rubric criterion)

**Done when:** the build runs on another PC from Main Menu to Victory without errors, and the video and document are ready to submit.

---

## Cut List (use only if a phase falls behind)

Cut in this order. Nothing in the grading rubric is ever cut.

1. Boss Phase 2 shockwave stomp
2. Randomized breaker order (use a fixed order)
3. Wing B crate jumping (use a raised catwalk to the drain controls)
4. Pylon reboot cycle (pylons stay destroyed after one stun)
5. Crate break-apart effect (crate disappears and drops its item)
6. Accuracy stat on the Victory screen
7. If the run is too long: make the gauge lock door start open, or shrink the junction box to a 2×2 grid
