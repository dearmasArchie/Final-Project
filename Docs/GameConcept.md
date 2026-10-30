# Game Concept Document: Project Outpost: Signal Lost

**Course:** IT 402W – Final Project  
**Institution:** Bulacan State University – College of Information and Communications Technology  
**Student Name(s):** \[Insert Name(s)\] & \[Partner's Name\]  
**Course, Year & Section:** \[Insert Course, Year & Section\]  
**Date:** \[Insert Date\]

---

## 1. Executive Summary & Project Overview

| Item | Detail |
| :---- | :---- |
| **Working Title** | Project Outpost: Signal Lost |
| **Genre** | First-Person Action Shooter with light puzzle and survival elements |
| **Perspective** | First-Person (FPP) |
| **Target Playtime** | 12–15 minutes on a first playthrough (to be re-timed after the puzzle additions; see section 3) |
| **Engine** | Unity 3D (Universal Render Pipeline) |
| **Scenes** | `MainMenu` + two gameplay scenes (`IslandExterior`, `UndergroundFacility`) |
| **Platform** | PC (Windows), mouse and keyboard |
| **Team / Schedule** | 2 developers, 5 days |

### High Concept Pitch

A lightning strike corrupts OVERSEER, the security AI of island research station Outpost 14. It turns every security drone and robot guard on the island against its people, cuts the long-range radio, and starts a 20-minute countdown to a full security purge. As a stranded combat engineer armed with only an experimental Arc Pistol, you must recover three override fuses from the island and its flooded bunker, restore the radio tower to call for rescue, and fight past OVERSEER's last line of defense, the siege mech Unit 00-Titan, to reach the extraction boat.

### Design Pillars

1. **Readable combat.** Every bot shows its state through a colored sensor light (a drone's eye or a robot's visor: blue, yellow, red), and every weak point glows. The player should never die without understanding why.
2. **Two modes, one gun.** The rapid Pulse is for speed and precision; the charged Overload Beam is for shields, barrels, and the boss. Every major encounter gives a reason to switch.
3. **Always one clear goal.** One objective at a time, shown on the HUD and spoken over the radio, with the purge timer adding constant pressure.

### Puzzle Overview

Simple puzzles break up the combat, each a different kind of thinking. All randomized ones change every run.

| Puzzle | Where | Type | Randomized | Unlocks |
| :---- | :---- | :---- | :---- | :---- |
| Armory code (color lockers + lights-off digit) | Maintenance bunker (island) | Observation | Yes | Armory → Arc Pistol |
| Main power lever (guarded) | Generator bunker (island) | Combat objective | – | Freight elevator down |
| Gauge lock (3 pressure gauges) | Wing A laser corridor | Observation | Yes | Breaker room door |
| Breaker sequence (order from the log) | Wing A breaker room | Read and follow | Yes | Fuse 1 pod |
| Pipe routing (schematic + junction levers) | Wing B flooded generator | Spatial logic | No | Drains the water → quarantine door |
| Junction box wiring (3×3 wire tiles) | Underground hub | Spatial logic | Yes | Elevator back up |

### Core Gameplay Loop

**Receive objective (radio) → Travel and fight (shooting) → Overcome a challenge (puzzle, hazard, or defense) → Collect a fuse or reward → Checkpoint → Next objective**

---

## 2. Story, Objective & Win/Lose Conditions

### Backstory

Outpost 14 is an isolated island facility that researches energy weapons. You are a combat engineer who arrived on the morning supply run. Minutes after you land, a lightning strike corrupts OVERSEER, the facility's security AI. It flags all personnel as intruders, turns its security drones and humanoid robot guards hostile, shuts down the radio tower (the "lost signal"), and starts a **20-minute Purge Protocol** that will turn the island's defense grid on everything still alive. The supply boat flees without you.

You shelter in the coastal safe room with **Dr. Reyes**, an injured scientist who cannot walk. She explains the only way out: the radio tower needs three override fuses to transmit a distress signal. Two fuses are in the underground utility wings; the third is held at the tower compound. Once the signal is out, a rescue boat can reach the pier. Dr. Reyes stays in the safe room and guides you by radio for the rest of the game.

When the signal goes out, OVERSEER deploys its final defense, **Unit 00-Titan**, to hold the pier and stop the extraction.

### Objective Chain

1. Talk to Dr. Reyes in the safe room. The armory is keypad-locked; the code is hidden in the old maintenance bunker ~100 m northwest of the safe room.
   Solve the bunker's code puzzle (see section 4), enter the code at the armory keypad, and take the Arc Pistol.
2. The freight elevator has no power. Fight to the **generator bunker** east of the road (guarded by 2 Enforcers and 2 Recon Stingers) and turn the main power lever on.
3. Ride the now-powered freight elevator down to the Underground Facility.
4. **Wing A:** Read the three pressure gauges along the laser corridor, disable the laser tripwires, enter the gauge code to open the breaker room, and solve the breaker sequence puzzle to collect **Fuse 1**.
5. **Wing B:** Cross the electrified floodway on crates, route the water to the drain with the junction levers and pump, and survive the 45-second quarantine lockdown to collect **Fuse 2**.
6. **Hub:** Rewire the elevator junction box so the freight elevator can go back up.
   *(Wings A and B and the junction box can be done in any order. The elevator back up needs both fuses and the rewired junction box.)*
7. Return to the surface, fight up the hill trail, and defeat the Aegis Warden guarding the tower compound to collect **Fuse 3**.
8. Insert all three fuses into the radio tower console and defend the compound for 90 seconds while the dish aligns and uploads the distress signal.
9. Take the service road down to the pier and destroy Unit 00-Titan.
10. Board the extraction boat.

### Win Condition

Board the extraction boat after Unit 00-Titan is destroyed. This triggers the Victory screen.

### Lose Conditions

* **Health depleted (0 HP):** Enemy attacks or hazards reduce your HP to 0.
* **Purge timer expires:** The 20:00 countdown reaches 00:00 before you board the boat.

Both trigger the Game Over screen, which offers **Retry from Checkpoint**, **Restart Game**, and **Main Menu**.

### Checkpoints

A checkpoint saves a snapshot of HP, ammo, fuses, remaining timer, and completed objectives. Retrying restores that snapshot exactly, so a death near the end does not force a full 15-minute replay.

| # | Location | Reached when |
| :---- | :---- | :---- |
| CP1 | Underground Central Hub | Arriving underground |
| CP2 | Underground Central Hub | Returning to the hub with the first fuse |
| CP3 | Surface freight elevator house | Returning to the surface with Fuses 1 and 2 |
| CP4 | Pier entrance | Distress signal upload completes |

---

## 3. Scene Architecture & Playable Loop

The build contains three scenes (`0: MainMenu`, `1: IslandExterior`, `2: UndergroundFacility`). A persistent `GameManager` singleton carries state between them.

| Phase | Scene | Time | Content | Ends when |
| :---- | :---- | :---- | :---- | :---- |
| **1. Safe Room & Island Road** | IslandExterior | 0:00–4:30 | Briefing, maintenance bunker code puzzle, armory, Arc Pistol. Fight the guards at the generator bunker and restore power. Fight Recon Stingers and Enforcer Units along the road. Learn weak-point crits and explosive barrels. | Player uses the freight elevator |
| **2. Underground Facility** | UndergroundFacility | 4:30–11:00 | Wing A gauge lock, laser corridor, and breaker puzzle (Fuse 1). Wing B electrified floodway, pipe routing, and quarantine ambush with the first Aegis Warden (Fuse 2). Hub junction box wiring. | Player rides the elevator up with 2 fuses |
| **3. Radio Tower** | IslandExterior | 11:00–14:30 | Reinforcement patrol on the hill trail. Aegis Warden guards Fuse 3. Insert fuses, survive the 90-second, three-wave defense. | Upload reaches 100% |
| **4. Pier Boss & Extraction** | IslandExterior | 14:30–17:00 | A gate opens onto the downhill service road to the pier. Two-phase Unit 00-Titan fight. Board the boat. | Player boards the boat |

With the puzzle additions, a first run is estimated at **15–17 minutes**. The purge timer stays at 20:00 for now; it will be re-tuned (e.g. to 22:00) after a full timed playthrough so first-time players keep a buffer of a few minutes.

**Returning to `IslandExterior`:** Phase 1 enemies stay destroyed (the `GameManager` remembers cleared encounters by ID). A new reinforcement patrol spawns on the hill trail instead, and the player spawns at the elevator house rather than the safe room.

### Teaching Order

Each mechanic is introduced safely before it is tested under pressure.

| Mechanic | Introduced | Tested |
| :---- | :---- | :---- |
| Pulse fire and weak-point crits | Phase 1 road (single Enforcer on patrol) | Every fight after |
| Explosive barrels | Phase 1 road (barrel next to a patrol group) | Quarantine room, tower defense |
| Precision shooting (laser emitters) | Wing A corridor | Boss plasma orbs |
| Overload Beam vs. shields | Quarantine room (Aegis Warden, barrels as a backup) | Tower Aegis Warden, boss Phase 1 |
| Shooting environment targets | Laser emitters, barrels | Boss pylons |

---

## 4. Player Interaction Mechanics & Bunker Challenges

The player interacts through a 3-meter camera raycast. Any object implementing `IInteractable` shows a context prompt (`[E] Pull PUMP (test routing)`).

| # | Interaction | Description |
| :---- | :---- | :---- |
| 1 | **NPC dialogue** | Dr. Reyes opens a briefing panel (5 lines, E to advance) and sends the player to the maintenance bunker. She speaks by radio afterwards. |
| 2 | **Doors and keypads** | Wall keypads open sliding airlocks. Locked doors show a red light and the reason (`Requires: Arc Pistol`). |
| 3 | **Freight elevator** | Scene transition with a fade. Going down needs the Arc Pistol and main power; going up needs Fuses 1 and 2 and the rewired junction box. A light above each panel shows red (locked) or green (ready). |
| 4 | **Pickups and fuse console** | Override Fuses, Battery Clips (+12 ammo), Medkits (+35 HP, max 100). Fuses are placed into the radio console one at a time. |
| 5 | **Breaker sequence puzzle** (Wing A) | See below. |
| 6 | **Junction levers + pump** (Wing B) | Set three levers up/down per the pipe schematic, then pull PUMP to test. See below. |
| 7 | **Code keypads** | Armory (4 digits) and Wing A breaker room (3 digits). Opens a code panel; type the digits (Backspace to correct, Esc to cancel). ACCESS DENIED on a wrong code. |
| 8 | **Light switch** (maintenance bunker) | Turns the bunker lights on and off; needed to see the hidden digit. |
| 9 | **Main power lever** (generator bunker) | Powers the freight elevator. The elevator panel shows a red light and "No power" until then, green after. |
| 10 | **Wire tiles** (hub junction box) | Each tile rotates 90° with E; connect IN to OUT to restore the elevator circuit. See below. |

### Maintenance Bunker: Armory Code Puzzle

A small concrete bunker ~100 m northwest of the safe room, reached unarmed along a winding dirt path that stays away from the robot patrols. Everything is randomized each run, so the code can't be memorized:

* **Wall message** (west wall): the color order, e.g. *"ARMORY CODE – YELLOW - BLUE - GREEN - RED"*, and the hint *"some things only show in the dark"*.
* **Four colored lockers** (blue, red, green, yellow), each stenciled with one digit.
* **One locker's digit is scratched off** (shown as #). It is also painted in glow paint, visible only when the **light switch** turns the lights off.
* **Decoy numbers** (BAY 7, SHELF 3, UNIT 21) are painted around the room to mislead.
* The code is the four locker digits read in the wall's color order. A radio line plays on entry: *"don't trust the first number you see."*

### Wing A: Gauge Lock, Laser Corridor & Breaker Puzzle

* **Gauge lock:** The breaker room is sealed by a door with a **3-digit keypad**. Three **pressure gauges** (PRESSURE 1, 2, 3) are mounted along the corridor wall, spread between the laser sets; each needle points at a number from 0 to 9, random each run. A plate by the keypad reads *"CODE = PRESSURE READINGS, FIRST TO LAST."* The player reads the needles (not painted digits) and has to get past the lasers to reach the keypad.
* **Laser corridor:** Red laser beams cross a narrow hallway. Touching a beam deals 25 damage and sounds an alarm that spawns one Recon Stinger. Shooting the wall-mounted emitter boxes switches the beams off.
* **Breaker puzzle:** Fuse 1 is locked in a glass pod wired to three wall breakers labeled **COOLANT**, **MAIN**, and **AUX**. The correct order is picked at random each run (one of six orders) and written in a maintenance log on a terminal at the start of the laser corridor. The player has to find and read it.
* **Wrong order:** An alarm sounds, the levers reset, and one Enforcer Unit marches in through a side door (at most two alarm Enforcers alive at once).
* **Correct order:** The pod opens and Fuse 1 can be collected.

### Wing B: Flooded Generator & Quarantine Room

* **Electrified floodway:** The lower generator floor is covered in sparking, ankle-deep water dealing 10 HP per second. The HUD flashes a hazard warning. Metal crates spaced no more than 2 meters apart lead to the drain controls on the north wall.
* **Pipe routing:** A **pipe schematic** is painted on the wall above three **junction levers (A, B, C)**, each set UP or DOWN. At each junction the schematic shows one branch going to RETURN (back into the room, red) and one continuing toward the **DRAIN** (blue). Each junction is marked UP and DOWN on the schematic, and the lever labels show their state (e.g. "A: DOWN"). **The pipes along the current route light up** as levers are flipped, so the player sees where the water will go before testing. The solution is A = DOWN, B = UP, C = DOWN. Pulling the **PUMP** handle sends the water: reaching the drain empties the floodway and unlocks the quarantine door; otherwise a sparking surge comes back with *"WATER RETURNED AT JUNCTION B"* (naming the junction) and the player adjusts.
* **Quarantine lockdown:** Picking up Fuse 2 seals the blast doors, sounds a siren, and starts a 45-second HUD countdown. Two Enforcer Units and one Aegis Warden step out of wall charging alcoves. Two red barrels in the room give a backup way to break the Aegis shield. The doors unlock when all enemies are destroyed or the countdown ends.

### Hub: Elevator Junction Box

* OVERSEER scrambled the elevator's control circuit. A **junction box** on the hub wall, next to the elevator corridor, holds a **3×3 grid of wire tiles** (straight and corner pieces).
* Looking at a tile and pressing **E** rotates it 90°. The wire glows where power flows; connecting the **IN** side to the **OUT** side restores the circuit (*"ELEVATOR CIRCUIT RESTORED"*), and Dr. Reyes confirms over the radio.
* The route, the IN/OUT rows, and the scramble are random each time, and the grid is always solvable. The elevator back up needs both fuses **and** the restored circuit.

---

## 5. Offensive Mechanic & Combat System

### Weapon: Dual-Mode Arc Pistol

| Mode | Input | Behavior | Damage | Ammo |
| :---- | :---- | :---- | :---- | :---- |
| **Pulse** | Left-click (hold for auto) | Fast electric projectile, 4 shots per second | 20 body / 45 weak-point crit (+0.5 s stun) | 1 |
| **Overload Beam** | Hold right-click 1.0 s, release | Hitscan beam. Breaks shields, detonates barrels. Releasing early cancels at no cost. 2-second cooldown after firing. | 60 | 4 |

* **Magazine:** 12 rounds. `R` reloads in 1.5 seconds; it reloads automatically when empty. Reserve holds up to 48.
* **No soft-locks:** Ammo lockers in the underground hub, a supply crate in the tower compound, and a crate at the pier all refill over time, so the player can never be stuck without ammo.
* **Feedback:** Hit markers on the crosshair (a red marker for crits), sparks on impact, a bright flash and screen shake when the Overload Beam fires.

### Destructible Environment

* **Wooden supply crates:** Break apart when shot and drop a Battery Clip (60%), a Medkit (25%), or nothing (15%).
* **Red hazard barrels:** Explode for 75 damage within 5 meters. The blast also hurts the player (half damage), so placement matters.
* **Laser emitters:** One hit disables the emitter and its beams.

---

## 6. AI Implementation & Final Boss

OVERSEER controls two kinds of machines:

* **Security drones** (Recon Stinger) hover at chest height and fly fast. Their weak point is the glowing camera eye.
* **Humanoid robot guards** (Enforcer Unit, Aegis Warden) walk and run like soldiers and carry arm-mounted laser rifles. Their weak point is the glowing head visor, so aiming for the head is rewarded the way players expect.

All enemies use `NavMeshAgent` movement controlled by a finite state machine. The sensor light (the drone's eye or the robot's visor) shows each bot's state. Humanoid robots play Mixamo animations driven by the state machine through Animator parameters (`Speed`, `Shoot`, `Hit`, `Die`).

| State | Light | Behavior |
| :---- | :---- | :---- |
| **Patrol** | Blue | Moves between waypoints. Scans a 15 m, 90° vision cone with a line-of-sight raycast (walls block vision). |
| **Chase** | Yellow | Triggered by seeing the player, being hit, or hearing gunfire within 15 m. Plays an alert beep and pings every bot within 15 m, which also switch to Chase. Moves to the player's last known position. If the player stays out of sight for 5 s, the bot returns to Patrol. |
| **Attack** | Red | Within attack range with line of sight. The light flashes for 0.4 s before each shot as a telegraph, then the bot fires. |
| **Stunned** | Flickering | 0.5 s after a weak-point crit. No movement or firing (robots play a hit-reaction stagger). |

Enemy lasers are visible projectiles (25 m/s), not hitscan, so the player can dodge them by strafing.

### Enemy Roster

| Enemy | HP | Speed | Attack Range | Attack | Weak Point / Counter |
| :---- | :---- | :---- | :---- | :---- | :---- |
| **Recon Stinger** (drone) | 40 | 6.0 | 4 m | Flies at you in a zig-zag, shock beam 12 dmg per second while in range | One eye crit destroys it |
| **Enforcer Unit** (humanoid robot) | 100 | 3.5 | 10 m | Arm-mounted laser rifle: 3-round burst, 6 dmg per bolt, every 2 s. Side-steps when hit. | Visor headshots; barrels for groups |
| **Aegis Warden** (heavy humanoid robot) | 180 | 2.5 | 12 m | Shoulder cannon: slow heavy plasma bolt, 20 dmg with small splash, every 2.5 s | Carries a large energy riot shield on its left arm that blocks Pulse from the front. Overload Beam or a barrel blast breaks it for 6 s, then it regenerates. Flanking also works. |

### Final Boss: Unit 00-Titan (500 HP, Coastal Pier)

The arena is the pier, with shipping containers for cover, two power pylons, and a refilling ammo crate. A boss health bar appears at the top of the HUD.

**Phase 1: Riot Shield (500 → 250 HP)**

* The Titan advances slowly behind a front riot shield. Every 5 seconds it launches three slow plasma orbs (15 damage each) that the player can shoot down.
* An Overload Beam knocks the shield down for 6 seconds and exposes two glowing cyan shoulder vents. In Phase 1 the Titan only takes damage through the vents.

**Phase 2: Pylon Tether (250 → 0 HP)**

* The Titan backs up to the end of the pier. Laser tethers connect it to two power pylons and make it invulnerable. Two Recon Stingers spawn.
* Every 6 seconds it stomps, sending out an expanding shockwave ring the player must jump over (20 damage if hit).
* Destroying both pylons (100 HP each) stuns the Titan for 5 seconds and exposes its chest core, which takes double damage. When the stun ends, the pylons reboot at half health and the cycle repeats. The numbers are tuned so Phase 2 takes about two cycles.
* At 0 HP the Titan collapses in an explosion, and the boat's ramp lowers.

---

## 7. 3D Environment & Map Layout

### Scene 1: `IslandExterior.unity`

A 250×250 m Unity Terrain bounded by water and cliffs. The playable route is a loop of about 600 m, so most of the island is scenery.

```
 N        [Zone 3: Radio Tower Compound]  <- Fuse 3, wave defense
              |   hill trail             \
              |                           \  service road (opens after the upload)
         [Zone 2: Freight Elevator House]  \
              |   dirt road                 |
 S   [Safe Room / Armory]  ---  [Zone 1: Pier, Boss Arena, Boat]
```

* **Zone 1 – Coast:** Safe room and armory, wooden pier over water, shipping containers, two power pylons, extraction boat.
* **Zone 2 – Island Road:** Dirt road through pine trees and rocks, explosive barrels near patrol routes, concrete elevator house.
* **Zone 3 – Radio Tower Compound:** Fenced hilltop with the steel radio tower, fuse console, supply crate, cover objects, and three wave spawn gates.

### Scene 2: `UndergroundFacility.unity`

A modular interior built from a sci-fi kit.

```
             [Wing A: Gauges + Laser Corridor] --(3-digit door)--> [Breaker Room / Fuse 1]
                    |
 [Elevator] -- [Central Hub: ammo locker, junction box, CP1/CP2]
                    |
             [Wing B: Flooded Generator + drain controls] --> [Quarantine Room / Fuse 2]
```

### Art Direction

Realistic PBR models lit by a photographed sunset sky (HDRI), with post-processing (tonemapping, bloom, ambient occlusion) for a grounded, cinematic look. Dusk lighting on the surface: an orange sky, long shadows, and the radio tower silhouetted on the hill as a constant landmark. The bunker is dark and cold, lit by hazard colors: red for lasers and alarms, cyan for electricity and fuses. Glowing elements (bot eyes, fuses, vents, pylons) use emissive materials with bloom so the important things stand out.

---

## 8. User Interface (UI) & Menus

### Heads-Up Display (HUD)

| Position | Element |
| :---- | :---- |
| **Top-left** | Red HP bar (`100 / 100`) and yellow stamina bar |
| **Top-center** | Purge timer (`20:00`), turns red under 2:00. Below it: signal upload bar (Phase 3) or `UNIT 00-TITAN` health bar (Phase 4) |
| **Top-right** | Current objective and fuse counter (`Override Fuses: 1 / 3`) |
| **Bottom-left** | Radio subtitles from Dr. Reyes with a portrait icon |
| **Bottom-right** | Ammo (`12 / 48`) and Overload charge/cooldown ring |
| **Center** | Crosshair with hit markers, interaction prompt, red screen-edge flash showing damage direction |
| **Pop-up** | "Checkpoint reached" message, hazard warning |

### Menu Screens

* **Main Menu:** Title, Play, Controls, Settings (mouse sensitivity, master/music/SFX volume), Credits, Quit.
* **Pause Menu (`Esc`):** Pauses the game (`Time.timeScale = 0`, cursor unlocked, audio paused). Resume, Retry from Checkpoint, Settings, Main Menu.
* **Game Over:** Cause of failure ("Killed by Enforcer Unit" or "Purge Protocol executed"). Retry from Checkpoint, Restart Game, Main Menu.
* **Victory:** Completion time, time remaining, enemies destroyed, accuracy. Main Menu.

### Controls

| Action | Input |
| :---- | :---- |
| Move / Look | WASD / Mouse |
| Sprint / Jump | Left Shift / Space |
| Pulse Fire / Overload Beam | Left-click / Hold right-click |
| Reload / Interact | R / E |
| Pause | Esc |

---

## 9. Audio Implementation

* **Music (2D), managed by a `MusicManager` with crossfades:** ambient synth for exploration, industrial percussion for combat and wave defense (switches when any bot is in Chase), a heavier track for the boss. Separate music and SFX volume via an Audio Mixer.
* **Sound effects:** dirt and metal footsteps by surface, Pulse shot, Overload charge hum and beam blast, reload click, hit and crit sounds, laser buzz, water crackle, crate break, barrel explosion, lockdown siren, radio static on each message, UI clicks.
* **3D spatial audio (`spatialBlend = 1`):** fuses and pylons hum, bots emit hover servos and a distinct alert beep when they spot you (an audio warning), and the Titan has heavy footsteps and a charge-up sound before each stomp.

---

## 10. Technical Architecture & Core C# Scripts

### Core Interfaces

```csharp
public interface IInteractable
{
    string Prompt { get; }       // e.g. "Turn Drainage Valve"
    bool CanInteract { get; }    // false shows the locked reason instead
    void Interact(GameObject interactor);
}

public enum DamageType { Pulse, Overload, Explosion, Hazard }

public interface IDamageable
{
    void TakeDamage(float amount, DamageType type, Vector3 hitPoint);
}
```

`DamageType` keeps shield rules simple: shields ignore `Pulse` and break on `Overload` or `Explosion`.

### Scripts

| Area | Script | Responsibility |
| :---- | :---- | :---- |
| **Game flow** | `GameManager.cs` | Persistent singleton (`DontDestroyOnLoad`). Fuses, purge timer, objective progress, cleared encounter IDs, checkpoint snapshots, stats. |
| | `ObjectiveManager.cs` | Ordered objective list. Updates the HUD and triggers Dr. Reyes's radio lines. |
| | `ScenePortal.cs` / `SpawnPoint.cs` | Elevator transitions with fade. Places the player at the matching spawn ID after loading. |
| **Player** | `FirstPersonController.cs` | Movement, sprint and stamina, jump, mouse look, surface footsteps. |
| | `PlayerHealth.cs` | `IDamageable`. HP, damage direction, death → Game Over. |
| | `PlayerInteraction.cs` | 3 m raycast for `IInteractable` objects and the prompt. |
| | `ArcPistol.cs` / `Projectile.cs` | Pulse projectiles, Overload charge beam, ammo, reload, effects. |
| **Enemies** | `EnemyAI.cs` | NavMesh state machine, vision cone, squad alert, sensor light, Animator parameters for humanoid robots. Per-enemy numbers in an `EnemyStats` ScriptableObject. |
| | `Health.cs` / `WeakPoint.cs` / `EnergyShield.cs` | Shared HP, crit multiplier on eye and visor colliders, shield logic. |
| | `BossController.cs` | Titan phases, shield, orbs, tethers, stomp, stun windows. |
| | `WaveSpawner.cs` | 90-second upload and three timed waves. |
| **World** | `LaserTripwire.cs`, `ElectrifiedWater.cs`, `BreakerPuzzle.cs`, `LockdownRoom.cs` | One script per bunker challenge, so each can be built and tested alone. |
| | `ArmoryCodePuzzle.cs`, `LightSwitch.cs`, `CodeKeypad.cs` | Maintenance bunker clues, the lights-off digit, and the code keypads (armory and breaker room). |
| | `PowerSwitch.cs`, `Gauge.cs` | Generator bunker main power lever; Wing A pressure gauges. |
| | `PipeRouting.cs`, `PipeLever.cs`, `PumpHandle.cs` | Wing B drain controls (schematic levers and pump). |
| | `WiringPuzzle.cs`, `WireTile.cs` | Hub junction box: random solvable 3×3 wire grid. |
| | `Destructible.cs`, `ExplosiveBarrel.cs`, `Pickup.cs`, `Door.cs` | Crates and drop tables, barrel blasts, pickups, keypad doors. |
| | `LightningLine.cs` | Jittering LineRenderer for the Overload Beam, Titan tethers, and electric sparks. |
| **UI / Audio** | `HUDController.cs`, `MenuController.cs`, `MusicManager.cs` | HUD updates, menu screens, music states. |

**Cross-scene state:** Each gameplay scene has its own player prefab. On load, the player reads HP and ammo from the `GameManager` and writes them back before leaving. This is simpler and less error-prone than carrying the player object itself across scenes.

---

## 11. IT 402W Grading Rubric Alignment

| Criteria | Points | Signal Lost Implementation |
| :---- | :---- | :---- |
| **1. 3D Game & Core Gameplay** | 15 | 12–15 minute first-person action loop across 2 gameplay scenes: shooting, puzzles, hazards, wave defense, and a two-phase boss, with checkpoints. |
| **2. Player Interaction** | 10 | 11 interaction types through `IInteractable`: NPC briefing, code keypads (armory, breaker room), light switch, main power lever, keypad doors, elevator scene transition, pickups and fuse console, breaker puzzle, junction levers + pump, wire tiles. Six puzzles in total (see the Puzzle Overview). |
| **3. Offensive/Attack Mechanic** | 10 | Dual-mode Arc Pistol (Pulse + charged Overload Beam), reloads, weak-point crits (drone eyes, robot visors) with stun, shield breaking, destructible crates, explosive barrels, laser emitters. |
| **4. 3D Environment & GameObjects** | 10 | Sculpted island terrain with water, roads, and landmarks, plus a modular two-wing bunker with hazards. |
| **5. AI Implementation** | 15 | 3 enemy types (a flying drone and two animated humanoid robots) on `NavMeshAgent` with Patrol/Chase/Attack/Stunned states, vision cones with line of sight, gunfire hearing, squad alerts, attack telegraphs, and a two-phase boss. |
| **6. User Interface (UI)** | 10 | Full HUD (HP, stamina, timer, upload/boss bar, objectives, radio subtitles, ammo, prompts, damage direction) and 4 menu screens with settings. |
| **7. Audio Implementation** | 5 | Music that changes with game state, surface footsteps, weapon and hazard SFX, 3D spatial audio on bots, fuses, pylons, and the boss. |
| **8. Objective & Win/Lose Conditions** | 10 | Clear objective chain (3 fuses → tower defense → boss → boat), lose on 0 HP or purge timer, checkpoint retry. |
| **9. Technical Implementation** | 10 | Interfaces (`IInteractable`, `IDamageable`), ScriptableObject enemy data, persistent cross-scene state, checkpoint snapshots, one script per system. |
| **10. Presentation, Polish & Creativity** | 5 | Dusk lighting and bloom, readable sensor lights, radio storytelling, particle effects, scene fades, credits screen. |
| **TOTAL** | **100** | |

---

## 12. 5-Day Two-Partner Execution Plan

### Roles

* **Partner A – Gameplay:** player controller, Arc Pistol, enemy AI, boss, combat balance.
* **Partner B – World & Systems:** scenes and level layout, `GameManager`, interactions and bunker challenges, UI, audio.

### Working Rules

* Same Unity version on both PCs. Set up Git with a Unity `.gitignore`, and set Asset Serialization to **Force Text**.
* **Never edit the same scene at the same time.** Partner A builds and tests in a private `Sandbox_A` scene and delivers prefabs. Partner B owns the two gameplay scenes and places those prefabs.
* Merge at the end of every day, then play the build together for 10 minutes.

### Schedule

| Day | Partner A (Gameplay) | Partner B (World & Systems) | End-of-day milestone |
| :---- | :---- | :---- | :---- |
| **1 – Foundation** | Project setup, first-person controller, interaction raycast, Arc Pistol Pulse + reload, `IDamageable`, test dummy | Graybox both scenes (ProBuilder), bake NavMesh, `GameManager`, elevator scene transition, spawn points, HUD skeleton | Walk from the safe room to the bunker and back; shoot a dummy |
| **2 – Combat & Interactions** | `EnemyAI` state machine, vision, squad alert; Enforcer (Robot Kyle + Mixamo Animator) and Stinger; Overload Beam; barrels and crates | Doors and keypads, pickups, NPC dialogue, laser tripwires, breaker puzzle, electrified water and valve | Phases 1 and 2 playable in graybox |
| **3 – Full Loop** | Aegis Warden and shield, quarantine room spawns, wave spawner, boss Phase 1 | `ObjectiveManager` and radio lines, purge timer, checkpoints, fuse console, all menu screens | Game playable start to finish |
| **4 – Boss & Art Pass** | Boss Phase 2, enemy hit effects, balance pass | Replace graybox with assets, paint terrain, trees, dusk lighting, water, audio and music states | **Content complete. Feature freeze.** |
| **5 – Polish & Ship** | Playtest and fix bugs | Playtest and fix bugs | Windows build tested on another PC, backup gameplay video recorded, document finalized |

On Day 5, time several full playthroughs and tune the numbers so a first run lands in 12–15 minutes. No new features after Day 4.

### Cut List

If the Day 3 milestone slips, cut in this order. Nothing listed in the rubric table is ever cut.

1. Boss Phase 2 shockwave stomp
2. Randomized breaker order (use a fixed order)
3. Wing B crate jumping (use a raised catwalk to the drain controls)
4. Pylon reboot cycle (pylons stay destroyed after one stun)
5. Crate break-apart meshes (crate disappears and drops its item)
6. Accuracy stat on the Victory screen
7. If the run is too long: make the gauge lock door start open, or shrink the junction box to a 2×2 grid

---

## 13. Free Asset Shopping List

**Art style: realistic (PBR).** Models use physically based materials, and the scene is lit by a real sky photo (HDRI). Free realistic assets are scarcer than low-poly ones, so a few categories use a workaround (marked ⚠).

**Recommended setup: Unity 6 LTS + URP.** Realism comes mostly from lighting: add a Global Volume with Tonemapping (ACES), Bloom, Color Adjustments, and Vignette, and enable SSAO (Screen Space Ambient Occlusion) on the URP Renderer.

**Rules for every asset:**
* Add it to `Credits.txt` (name, author, link, license) as soon as it is imported. This becomes the Credits screen.
* **Licenses:** CC0 = no credit required. CC BY = credit required (name the author and link the model). Unity Asset Store EULA = free to use in your game, cannot be shared as raw files. Unity Companion License = free for Unity projects.
* **Keep the project small:** download Poly Haven assets at **2K**, not 4K or 8K, and set texture Max Size to 2048 in Unity. Use Git LFS for large files, or do not commit Asset Store packages (each partner imports them).
* **Pink materials** mean the asset was made for the Built-in pipeline. Fix with **Window > Rendering > Render Pipeline Converter**.
* Sketchfab downloads need a free account. Mixamo needs a free Adobe account.
* Prices and licenses can change. Check each page before downloading.

### Player & Weapon

| # | Category | Recommended Pick | Backup | License | Notes |
| :---- | :---- | :---- | :---- | :---- | :---- |
| 1 | **First-person controller** | Our own `FirstPersonController.cs` (about 150 lines, written on Day 1) | [Mini First Person Controller](https://assetstore.unity.com/packages/tools/input-management/mini-first-person-controller-174710) (free) | Our code / Asset Store EULA | Unity's official Starter Assets listings are now marked deprecated on the store. |
| 2 | **Arc Pistol model** | [Sci-fi Pistol \[PBR\]](https://assetstore.unity.com/packages/3d/props/guns/sci-fi-pistol-pbr-213679) (StarGames studio, 35 MB) | Search Sketchfab for "sci-fi pistol" with the Downloadable and Creative Commons filters | Asset Store EULA | Includes albedo, metallic, normal, AO, and emission maps. Make the emission glow cyan to sell the "arc" theme. Made in Unity 2020.1, so run the material converter. |

### Environment

| # | Category | Recommended Pick | Backup | License | Notes |
| :---- | :---- | :---- | :---- | :---- | :---- |
| 3 | **Graybox geometry** | ProBuilder (Package Manager, Unity) | Unity primitive cubes | Free | Block out both levels first; replace or texture on Day 4. |
| 4 | **Bunker walls, floors, doors** ⚠ | ProBuilder shapes + realistic textures from [Poly Haven](https://polyhaven.com/textures) and [ambientCG](https://ambientcg.com) (concrete, painted metal, metal plates, hazard stripes) | [Modular Sci-Fi Corridor](https://assetstore.unity.com/packages/slug/142811) (MagixBox, 299 MB, more sci-fi than realistic) | CC0 | No free realistic modular bunker kit was found. A concrete-and-steel utility bunker built in ProBuilder with scanned textures looks realistic and fits the story. |
| 5 | **Bunker & island props** | [Poly Haven models](https://polyhaven.com/models): `wooden_crate_01`, `wooden_military_crate`, `metal_tool_chest`, `metal_jerrycan`, industrial category (97 models) | [Crate & Barrel Bundle](https://opengameart.org/content/crate-barrel-bundle) (OpenGameArt) | CC0 | Scanned, realistic props. Use the crates as destructible supply crates. |
| 6 | **Explosive barrel** | Poly Haven [`Barrel_01`](https://polyhaven.com/a/Barrel_01) (a red metal oil drum) | Poly Haven `barrel_03` (blue steel drum, tint it red) | CC0 | Add a hazard decal or stripe so players recognize it as explosive. |
| 7 | **Medkit pickup** | Poly Haven [`medical_box`](https://polyhaven.com/a/medical_box) | A white box with a red cross texture | CC0 | Add a slow spin and a soft glow so it reads as a pickup. |
| 8 | **Pine trees** | [Conifers \[BOTD\]](https://assetstore.unity.com/packages/3d/vegetation/trees/conifers-botd-142076) (forst, 111 MB) | Poly Haven [`pine_tree_01`](https://polyhaven.com/a/pine_tree_01) and [`fir_tree_01`](https://polyhaven.com/a/fir_tree_01) | Asset Store EULA / CC0 | 4 realistic conifers from Unity's *Book of the Dead* demo, set up for the Terrain tree tool. Check that its shaders work in URP after import. |
| 9 | **Rocks and cliffs** | [Poly Haven rocks](https://polyhaven.com/models/rocks) (37 scanned rocks and boulders) | — | CC0 | Place along the coast and hill trail; they also hide the edges of the play area. |
| 10 | **Terrain textures** | [Poly Haven textures](https://polyhaven.com/textures): grass, forest ground, dirt road, rock, sand | [Terrain Textures – FREE](https://assetstore.unity.com/packages/2d/textures-materials/nature/terrain-textures-free-271990) (Rowlan, 705 MB, already set up as Terrain Layers) | CC0 / Asset Store EULA | Create a Terrain Layer from each albedo and normal map. 4–5 layers is enough. |
| 11 | **Sky and lighting (dusk)** | [Poly Haven HDRIs](https://polyhaven.com/hdris/sunrise-sunset), "sunrise-sunset" category (225 skies) | [Fantasy Skybox FREE](https://assetstore.unity.com/packages/slug/18353) | CC0 | Use a real photographed sunset as the skybox (Skybox/Panoramic material). It also lights the scene realistically. |
| 12 | **Ocean water** ⚠ | [Simple Water Shader URP](https://assetstore.unity.com/packages/slug/191449) (IgniteCoders) | A URP Shader Graph water we make: Poly Haven water normal maps, depth color, and Fresnel | Asset Store EULA | Free realistic URP ocean systems (like Crest) are paid. A dark, reflective, slowly moving surface at dusk looks convincing from a distance. |

### Characters & Enemies

| # | Category | Recommended Pick | Backup | License | Notes |
| :---- | :---- | :---- | :---- | :---- | :---- |
| 13 | **Recon Stinger** (fast drone) | [Buster Drone](https://sketchfab.com/3d-models/buster-drone-294e79652f494130ad2ab00a13fdbafd) (LaVADraGoN, Sketchfab) | [Robot Sphere](https://assetstore.unity.com/packages/3d/characters/robots/robot-sphere-136226) | CC BY 4.0 | Realistic PBR drone, 16.8k triangles, rigged and animated. Credit the author. |
| 14 | **Enforcer Unit** (humanoid robot) | [Robot Kyle \| URP](https://assetstore.unity.com/packages/3d/characters/robots/robot-kyle-urp-4696) (Unity Technologies) + [Mixamo](https://www.mixamo.com) animations (rifle idle, walk, run, shoot, hit reaction, death) | — | Unity Companion License | Kyle has a humanoid rig, so any Mixamo animation works on it. In Mixamo, download animations "Without Skin" and set each clip's rig to Humanoid. Attach a laser rifle (or a simple cylinder) to the right-hand bone. Put a sphere collider tagged `WeakPoint` on the head visor. |
| 15 | **Aegis Warden** (heavy humanoid robot) | Robot Kyle (#14) scaled to 1.3× with a dark armored material + Mixamo "Sword and Shield" animations (block idle, shield walk) | — | Unity Companion License | Reusing Kyle saves a full day of asset work. The shield is a transparent emissive mesh attached to the left forearm bone; it plus the size and color make the Aegis read as a different enemy. |
| 16 | **Unit 00-Titan (boss)** ⚠ | [Yellow Heavy Robot 8K](https://sketchfab.com/3d-models/yellow-heavy-robot-8k-downloadable-8fe08c70a9e1479e8d35d47e68b15afc) (Martark, Sketchfab) | [Walker (Rigged)](https://opengameart.org/content/walker-rigged) (Soleam, OpenGameArt, IK rig) | CC BY 4.0 / CC BY 3.0 | Realistic biped mech, but **not rigged** and heavy (128k triangles, 8K textures). Set textures to 2048. Try Mixamo's auto-rigger; if it fails, animate it in code (slow stomping advance, body sway). |
| 17 | **Dr. Reyes (NPC)** | [Mixamo](https://www.mixamo.com) character + a sitting or injured idle animation | — | Free with an Adobe account, royalty-free | Search the library for "sitting" or "injured". Download as FBX for Unity, set the rig to Humanoid. |
| 18 | **Extraction boat** ⚠ | [Small Motorboat](https://sketchfab.com/3d-models/small-motorboat-b488d632a4804cd783e9ef009976bcd4) (Tijmen Matthys, Sketchfab, 1.2k triangles) | Poly Haven [`ship_pinnace`](https://polyhaven.com/a/ship_pinnace) (realistic but an old wooden sailing boat) | CC BY 4.0 / CC0 | The weakest category: no free realistic rescue or patrol boat was found. Paid ones cost about $3–$30 if that is allowed. |

### Effects, Audio & UI

| # | Category | Recommended Pick | Backup | License | Notes |
| :---- | :---- | :---- | :---- | :---- | :---- |
| 19 | **Explosions, fire, sparks** | [Particle Pack](https://assetstore.unity.com/packages/vfx/particles/particle-pack-127325) (Unity Technologies) | — | Asset Store EULA | Realistic effects. URP support on Unity 2022.3 or newer. |
| 20 | **Electricity / lightning** | Our own `LightningLine.cs`: a LineRenderer with jittering points and an emissive material | Particle Pack sparks (#19) | Our code | The free URP lightning packs found were Built-in-only or HDRP-only. |
| 21 | **Weapon sounds** | [Free Laser Weapons](https://assetstore.unity.com/packages/audio/sound-fx/weapons/free-laser-weapons-214929) (Daniel SoundsGood) | [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) (Kenney, 70 files) | Asset Store EULA / CC0 | About 50 laser shots and reloads. Kenney's pack covers bot lasers, alarms, and hums. |
| 22 | **Impacts, footsteps, crates** | [Impact Sounds](https://kenney.nl/assets/impact-sounds) (Kenney, 130 files) | [freesound.org](https://freesound.org) (check each license) | CC0 | Footsteps, wood breaks, and metal hits. |
| 23 | **UI sounds** | [Interface Sounds](https://kenney.nl/assets/interface-sounds) (Kenney, 100 files) | — | CC0 | Button clicks, objective updates, checkpoint chime. |
| 24 | **Music: exploration & combat** | [Sci-Fi Music Loops Pack](https://assetstore.unity.com/packages/audio/music/electronic/sci-fi-music-loops-pack-120186) (Robson Cozendey, 350 MB) | [Universe Sounds Free Pack](https://assetstore-fallback.unity.com/packages/audio/ambient/sci-fi/universe-sounds-free-pack-118865) (1 battle + 1 menu track) | Asset Store EULA | Listen first: the store page does not list the tracks. Pick one calm loop and one intense loop. |
| 25 | **Music: boss** | ["Delta"](https://opengameart.org/content/delta) (OpenGameArt, sci-fi boss fight) | ["Boss Battle Loop"](https://opengameart.org/content/boss-battle-loop) or the ["Good CC0 Music"](https://opengameart.org/content/good-cc0-music) collection on OpenGameArt | CC0 | Check that the track loops cleanly before using it. |
| 26 | **HUD and menu graphics** | [UI Pack – Sci-Fi](https://kenney.nl/assets/ui-pack-sci-fi) (Kenney, 130 files) | Unity's built-in UI sprites | CC0 | Panels, buttons, sliders, and bars. Flat UI suits a realistic game; keep colors muted (white, cyan, red). |
| 27 | **Font** | [Rajdhani](https://fonts.google.com/specimen/Rajdhani) (Google Fonts) | [Orbitron](https://fonts.google.com/specimen/Orbitron) | SIL Open Font License | A clean military-style font. Generate a TextMeshPro font asset (**Window > TextMeshPro > Font Asset Creator**). |
