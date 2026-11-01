# IT 402W – Final Project: Requirements Checklist

Source: *IT 402W – Final Project* instructional material (CICT, Bulacan State University).
Game: **Project Outpost: Signal Lost** (Unity 6, URP, first-person) · Team **WeFinds**

**Objective from the instructions:** a functional 3D game showing player interaction, combat, AI, user interface, audio and 3D environment design.

Status key: ✅ done · 🟡 done, needs a human run or a final check · ⬜ still to do

---

## A. Project Requirements

| # | Requirement | Status | How the game meets it |
|---|---|---|---|
| 1 | **Create a 3D Game** (Unity, first- or third-person, own genre/theme/story) | ✅ | Unity 6 first-person action-puzzle game. Story: a lightning strike corrupts the island AI OVERSEER, which starts a 20-minute Purge Protocol; the player must restore the radio signal and escape by boat. A story introduction plays between Play and the game start. |
| 2 | **Player Interaction** (doors, pickups, switches, NPCs) | ✅ | Press **E** on: keypad-locked doors (safe room armory, fence gate, Wing A and B doors, breaker room, maintenance terminal), light switch, main power switch/lever at the generator bunker, freight elevator switch, junction-box levers and pipe levers in Wing B, pump handle, breaker and wiring puzzles, pressure gauges, the **fuse console** at the radio tower, ammo lockers, medkits, battery and fuse pickups, the Arc Pistol pickup, the radio, and the **NPC Dr. Reyes** (dialogue, then radio guidance). |
| 3 | **Offensive / Attack Mechanic** (melee, shooting, magic) with a noticeable effect | ✅ | **Arc Pistol** with two modes: rapid **Pulse** shots (hold left click) and a charged **Overload Beam** (hold right click, release). Hits show hit markers, sparks and damage; enemies take damage, flinch and die with an explosion; the Overload Beam breaks the Aegis Warden's shield and detonates explosive barrels. Ammo, reload and recoil/bob animation on the gun. |
| 4 | **3D GameObjects and Environment** (own models, downloaded assets, Unity assets, properly integrated) | ✅ | Many 3D objects built and placed with editor builders: radio tower, bunkers, buildings, fence, pier, boat, underground facility (rooms, pipes, ducts, doors, lamps, equipment), props. Downloaded assets are integrated and credited: Poly Haven models and textures, Sketchfab models (drone, heavy robot, boat, battery, fuse), Asset Store packs (Sci-Fi Robots, Sci-fi Pistol, Conifers, particles), Dr. Reyes character model. All listed on the Credits screen. |
| 5 | **AI Implementation** (patrol, chase, attack) | ✅ | NavMesh enemies (`EnemyAI`): **Enforcers** and **Recon Stingers** patrol waypoints, notice the player, chase and attack (ranged bursts, shock beam). **Aegis Warden** carries a shield that only the Overload Beam or flanking breaks. **Unit 00-Titan** boss has phases, pylons, vents and a stomp/shockwave. **Dr. Reyes** is an AI-driven NPC that guides by radio. Enemies stop animating and are removed when dead. |
| 6 | **User Interface** (HP, score, inventory, objectives, timer) and menus (Main, Pause, Game Over) | ✅ | **HUD:** health bar, stamina, purge **timer**, current **objective**, fuse count, ammo, Overload charge ring, interaction prompt, hit markers, damage flash, dialogue and radio panels, code-entry panel, event progress bar. **Menus:** Main Menu (Play, Controls, Settings, Credits, Quit) with an island background, Pause, Game Over (Retry from Checkpoint, Restart, Main Menu), Victory screen with rank and stats, Settings (sensitivity, master/music/SFX volume), scrollable Credits. |
| 7 | **Audio** – background music | ✅ | `MusicManager`: exploration track, combat track, boss battle music, and a victory song. |
| 7 | **Audio** – sound effects | ✅ | Weapon and laser shots, impacts, explosions, alarms, door and keypad sounds, UI button clicks, robot servo/stomp/alert sounds, radio static, pickup sounds. |
| 7 | **Audio** – spatial sounds | ✅ | Enemy servos, alarms, lamps hum, generator, radio and rain/thunder are 3D audio sources, so volume and direction change with distance. Footsteps change with the ground surface. |
| 8 | **Game Objective and Win/Lose Conditions** | ✅ | **Objective chain:** get the armory code, take the pistol, power the elevator, collect three fuses (Wing A, Wing B, tower compound), upload the distress signal, destroy Unit 00-Titan, board the boat. **Win:** board the boat, which opens the Extraction Successful screen. **Lose:** HP reaches 0, or the 20:00 purge timer runs out, which opens Game Over with checkpoint retry. |
| 9 | **Playable Game Loop** (beginning to end, features work together) | 🟡 | Main Menu → intro → island → underground → island → boss → boat → Victory → Main Menu or Play Again. An automated smoke test passes. **Still needs:** full human play-throughs by each partner (Phase 5). |
| 10 | **Game Environment** (terrain, buildings, roads, water, hills) | ✅ | Terrain island with a hill trail up to the tower, service road to the pier, safe room, maintenance bunker, generator bunker, buildings, fence, forest, rocks, animated water around the island and flooded Wing B, a pier and extraction boat, and a sealed underground facility with a hub, two wings and corridors. |
| 11 | **Polish and Presentation** (placement, lighting, camera, materials, animations, audio, UI design, gameplay flow) | ✅ | Dusk sunset lighting, bloom, SSAO and fog; a storm (rain, darker light, lightning) when the boss fight starts; PBR textures and custom shaders (triplanar, water); skinned enemy animations (walk, attack, death), Titan walk, gun sway; styled UI with custom font and borders; scene fades; checkpoints and fall recovery. |

---

## B. Grading Rubric (100 points)

| # | Criteria | Pts | Status | Evidence in the game |
|---|---|---|---|---|
| 1 | **3D Game & Core Gameplay** | 15 | 🟡 | Complete first-person game with a clear concept; movement, sprint, jump, shooting and interaction work. Needs final human play-through to confirm "works smoothly". |
| 2 | **Player Interaction** | 10 | ✅ | Many different working interactions: keypad doors, switches and levers, elevator, fuse console, pickups, puzzles, NPC dialogue (see requirement 2). |
| 3 | **Offensive/Attack Mechanic** | 10 | ✅ | Two-mode Arc Pistol (Pulse and Overload Beam) integrated with enemy health, shields, barrels and the boss. |
| 4 | **3D Environment & GameObjects** | 10 | ✅ | Island and underground facility with terrain, models, materials, props and water. Third-party assets credited. |
| 5 | **AI Implementation** | 15 | ✅ | Patrolling, chasing and attacking enemies, shielded Aegis Warden, phased Titan boss, NPC Dr. Reyes. |
| 6 | **User Interface (UI)** | 10 | ✅ | Full HUD plus Main, Pause, Settings, Game Over, Victory and Credits screens. |
| 7 | **Audio Implementation** | 5 | ✅ | Music for exploration, combat, boss and victory; sound effects; 3D spatial audio; footsteps. |
| 8 | **Game Objective & Win/Lose Conditions** | 10 | ✅ | Clear objective chain, win by boarding the boat, lose by 0 HP or the purge timer; checkpoint retry. |
| 9 | **Technical Implementation & Functionality** | 10 | 🟡 | Organized scripts (Combat, Player, World, UI, Audio, Core), singleton managers, smoke test with no errors. Bug list from human play-tests still to be fixed. |
| 10 | **Presentation, Polish & Creativity** | 5 | ✅ | Story intro, island menu background, storm boss fight, victory screen with rank, credits, custom UI. |
| | **TOTAL** | **100** | | |

---

## C. Submission Checklist

| Item | Status | Notes |
|---|---|---|
| Student names, course/year/section and date on the document | 🟡 | Team names are in the credits and in `GameConcept.md`. Course, year, section and date still to fill in. |
| Game playable from beginning to end | 🟡 | Smoke test passes; each partner should play at least two full runs. |
| Windows build up to date | ⬜ | Existing build is from before the latest changes. Rebuild with the `build` command (~19 minutes), delete the `*_BurstDebugInformation_DoNotShip` folder, then test on a different PC. |
| Gameplay video as a backup for the presentation | ⬜ | Record a full run. |
| Credits list every asset and license | 🟡 | Authors and licenses still to confirm for the Sketchfab battery and fuse models and the Dr. Reyes model. |
| Presentation notes | ✅ | [PRESENTATION.md](PRESENTATION.md) |
| Game design document matches the final game | 🟡 | [GameConcept.md](GameConcept.md) updated; recheck it after the last changes. |
