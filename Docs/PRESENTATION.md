# Signal Lost – Presentation Notes

Rubric table: [GameConcept.md section 11](GameConcept.md). Build: `Builds/SignalLost/ProjectOutpost_SignalLost.exe` (Windows 64-bit, ~600 MB).

## 60-second pitch
Outpost 14's security AI has gone rogue. You are a combat engineer with 20 minutes to restore the radio tower, send a distress signal and escape by boat. Three fuses, six puzzles, three enemy types and a two-phase boss stand in the way.

## Demo route (about 6 minutes, using the checkpoint retry to skip ahead)
1. **Main Menu** – show Controls, Settings (sliders) and the scrolling Credits.
2. **Safe room** – Dr. Reyes' briefing and the objective text. Mention the radio subtitles.
3. **Maintenance bunker** – lights off to reveal the hidden glowing digit (puzzle 1), enter the code at the armory keypad, take the Arc Pistol.
4. **Generator bunker** – fight Enforcers and Stingers (patrol, chase, attack states; vision cone; squad alert), shoot the red barrel, pull the power lever.
5. **Underground** – Wing A laser corridor (weave through the gates), Wing B electrified floodway and pipe routing.
6. **Boss** – Titan Phase 1 (Overload Beam drops the shield, shoot the vents) and Phase 2 (pylons, shockwave, chest core).
7. **Victory screen** – time, enemies destroyed, accuracy.

If time is short, start from a checkpoint (die once on purpose to show Retry from Checkpoint).

## Key points per rubric criterion
- **Gameplay and objectives:** 10-step objective chain; lose on 0 HP or the 20:00 purge timer; four checkpoints that restore HP, ammo, fuses and timer.
- **Interaction:** one `IInteractable` interface, 11 interaction types, six different puzzles.
- **Combat and AI:** dual-mode Arc Pistol, weak-point crits, shield breaking; `EnemyAI` state machine on NavMesh, vision, hearing, squad alerts, telegraphed attacks.
- **Environment and art:** painted island terrain, custom water shader, triplanar bunker materials, dusk lighting and post-processing.
- **UI and audio:** full HUD and four menus; state-driven music, surface footsteps, 3D audio on bots and the boss.
- **Technical:** `IDamageable`, ScriptableObject enemy stats, persistent cross-scene `GameManager`, editor tools that rebuild scenes from code.

## Before presenting
- Fill in the team names (Credits.txt and the GameConcept header).
- Record the backup gameplay video.
- Test the build on the presentation PC.
