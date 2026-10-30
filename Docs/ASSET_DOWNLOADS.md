# Signal Lost – Asset Download List (Phase 4)

Sizes were looked up from each server on 2026-10-30. Everything in **Part A** is free and CC0 (public domain) unless noted, and can be downloaded by Claude with your approval. **Part B** needs your own accounts, so you download it. Every asset here also goes in `Credits.txt` / the Credits screen.

---

## Part A – Claude downloads with your approval (~164 MB total)

Saved into the project under `Assets/_Project/Art/PolyHaven/<asset>/` and `Assets/_Project/Audio/<pack>/`. Zip files are extracted into their own folders; nothing is executed.

### Poly Haven (polyhaven.com) – CC0, 2K resolution (~156 MB)

| # | Asset | Use | Files | Size |
| :---- | :---- | :---- | :---- | :---- |
| 1 | `aerial_grass_rock` | Terrain: grass | diffuse, normal, ARM (JPG) | 7.0 MB |
| 2 | `brown_mud_leaves_01` | Terrain: dirt roads and paths | diffuse, normal, ARM | 13.3 MB |
| 3 | `rocky_terrain_02` | Terrain: hill and cliffs | diffuse, normal, ARM | 8.9 MB |
| 4 | `coast_sand_rocks_02` | Terrain: beach and coast | diffuse, normal, ARM | 10.5 MB |
| 5 | `concrete_floor_worn_001` | Bunker floors | diffuse, normal, ARM | 1.9 MB |
| 6 | `concrete_wall_008` | Bunker and island bunker walls | diffuse, normal, ARM | 7.2 MB |
| 7 | `metal_plate` | Doors, panels, metal props | diffuse, normal, ARM | 8.7 MB |
| 8 | `belfast_sunset_puresky` | Sunset sky + scene lighting (HDRI) | 1 HDR | 4.4 MB |
| 9 | `boulder_01` | Rocks along the coast and trail | FBX + textures | 20.4 MB |
| 10 | `rock_moss_set_01` | Small rock clusters | FBX + textures | 10.0 MB |
| 11 | `Barrel_01` | Explosive barrel (red oil drum) | FBX + textures | 14.1 MB |
| 12 | `wooden_crate_01` | Breakable supply crates | FBX + textures | 11.0 MB |
| 13 | `wooden_military_crate` | Armory and bunker props | FBX + textures | 13.8 MB |
| 14 | `medical_box` | Medkit pickup | FBX + textures | 9.7 MB |
| 15 | `metal_jerrycan` | Generator bunker props | FBX + textures | 15.6 MB |

*Left out on purpose:* Poly Haven `pine_tree_01` (814 MB) and `fir_tree_01` (314 MB) are too heavy for a game; use **Conifers [BOTD]** from the Asset Store instead (Part B). `coast_rocks_05` (77 MB) is also skipped.

### OpenGameArt (opengameart.org) – audio (~7.5 MB)

| # | File | Use | License | Size |
| :---- | :---- | :---- | :---- | :---- |
| 16 | `boss_battle_loop.mp3` ("Boss Battle (loop)") | Titan boss music | CC0 | 2.35 MB |
| 17 | `sci-fi-sfx.zip` ("50 CC0 Sci-Fi SFX") | Weapon, laser, alarm, computer sounds | CC0 | 2.32 MB |
| 18 | `100-CC0-SFX_0.zip` ("100 CC0 SFX") | Impacts, explosions, UI clicks | CC0 | 2.79 MB |
| 19 | `[kdd]DifferentSteps_0.zip` ("Different steps on wood, stone, leaves, gravel and mud") | Footsteps (gravel for dirt, stone for bunker) | CC0 | 0.07 MB |

*Note:* the earlier asset list called "Delta" CC0, but its page says **CC-BY 4.0** (credit required). The boss track above is CC0 instead.

---

## Part B – You download (needs your accounts)

| Asset | Where | Use | Notes |
| :---- | :---- | :---- | :---- |
| Robot Kyle \| URP | Unity Asset Store (free) | Enforcer + Aegis Warden body | Add to My Assets, then import via Package Manager > My Assets |
| Mixamo animations | mixamo.com (free Adobe account) | Enforcer/Aegis: rifle idle, walk, run, shoot, hit reaction, death; Aegis: sword-and-shield idle/walk; Dr. Reyes: sitting/injured idle | Download as FBX for Unity, **Without Skin**; put them in `Assets/_Project/Art/Mixamo/` |
| Sci-fi Pistol [PBR] | Unity Asset Store (free) | Arc Pistol model | |
| Conifers [BOTD] | Unity Asset Store (free) | Pine trees | Replaces the heavy Poly Haven trees |
| Particle Pack | Unity Asset Store (free) | Explosions, fire, sparks | |
| Free Laser Weapons | Unity Asset Store (free) | Pulse and Overload sounds | |
| Sci-Fi Music Loops Pack | Unity Asset Store (free) | Exploration and combat music | Listen first; pick one calm and one intense loop |
| Buster Drone | Sketchfab (free account), CC BY 4.0 | Recon Stinger | Download FBX; credit the author |
| Yellow Heavy Robot 8K | Sketchfab, CC BY 4.0 | Unit 00-Titan | Download FBX; credit the author |
| Small Motorboat | Sketchfab, CC BY 4.0 | Extraction boat | Download FBX; credit the author |
| UI Pack – Sci-Fi (optional) | kenney.nl, CC0 | HUD/menu graphics | Kenney blocks scripted downloads, so this one is yours to click |

Put Part B files anywhere under `Assets/_Project/Art/` (or import the Asset Store packages normally) and tell Claude; it will wire them into the prefabs and scenes.
