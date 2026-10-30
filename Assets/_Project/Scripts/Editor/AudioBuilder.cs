using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace SignalLost.EditorTools
{
    /// <summary>
    /// Phase 4 audio pass. Creates Audio/MainMixer (Master > Music, SFX with exposed MusicVolume / SfxVolume) and
    /// Resources/AudioLibrary (every sound id -> clips). Safe to re-run: sounds already in the library keep
    /// whatever clips you assigned, only missing ids are added. Run with the <c>audio</c> bridge command.
    /// </summary>
    public static class AudioBuilder
    {
        const string AudioDir = "Assets/_Project/Audio";
        const string MixerPath = AudioDir + "/MainMixer.mixer";
        const string ResourcesDir = "Assets/_Project/Resources";
        const string LibraryPath = ResourcesDir + "/AudioLibrary.asset";

        const string General = AudioDir + "/SFX_General/";
        const string SciFi = AudioDir + "/SFX_SciFi/";
        const string Steps = AudioDir + "/Footsteps/";
        const string Generated = AudioDir + "/Generated/";
        const string Laser = "Assets/Laser Weapons Sound Pack/Free/";

        // Stand-in music until the final calm and intense tracks are picked from the Sci-Fi Music Loops Pack.
        const string ExplorationTrack = SciFi + "loop_ambient_01.ogg";
        const string CombatTrack = SciFi + "loop_machine_02.ogg";
        const string BossTrack = AudioDir + "/Music/boss_battle_loop.mp3";

        static readonly List<SoundEntry> entries = new List<SoundEntry>();

        public static void Build()
        {
            Directory.CreateDirectory(ResourcesDir);
            var mixer = BuildMixer(out var music, out var sfx);

            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.mixer = mixer;
            library.musicGroup = music;
            library.sfxGroup = sfx;
            if (library.exploration == null) library.exploration = Load(ExplorationTrack);
            if (library.combat == null) library.combat = Load(CombatTrack);
            if (library.boss == null) library.boss = Load(BossTrack);

            entries.Clear();
            DefineSounds();
            var merged = new List<SoundEntry>(library.sounds ?? new SoundEntry[0]);
            int added = 0;
            foreach (var entry in entries)
            {
                if (merged.Exists(e => e.id == entry.id)) continue;
                merged.Add(entry);
                added++;
            }
            library.sounds = merged.ToArray();

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log($"audio: mixer ok, library has {merged.Count} sounds ({added} added)");
        }

        // ------------------------------------------------------------------ mixer

        // The mixer editing classes are internal to UnityEditor, so they are reached through reflection.
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static readonly System.Reflection.Assembly EditorAssembly = typeof(Editor).Assembly;
        static readonly System.Type ControllerType = EditorAssembly.GetType("UnityEditor.Audio.AudioMixerController");

        static object Call(object target, string method, params object[] args) =>
            (target is System.Type t ? t : target.GetType()).GetMethod(method, Any, null, ArgTypes(args), null)
                .Invoke(target is System.Type ? null : target, args);

        static System.Type[] ArgTypes(object[] args)
        {
            var types = new System.Type[args.Length];
            for (int i = 0; i < args.Length; i++) types[i] = args[i].GetType();
            return types;
        }

        static AudioMixer BuildMixer(out AudioMixerGroup music, out AudioMixerGroup sfx)
        {
            var mixer = AssetDatabase.LoadAssetAtPath(MixerPath, ControllerType) as AudioMixer;
            if (mixer == null)
            {
                Call(ControllerType, "CreateMixerControllerAtPath", MixerPath);
                AssetDatabase.ImportAsset(MixerPath);
                mixer = AssetDatabase.LoadAssetAtPath(MixerPath, ControllerType) as AudioMixer;
            }

            music = FindGroup(mixer, "Music") ?? NewGroup(mixer, "Music");
            sfx = FindGroup(mixer, "SFX") ?? NewGroup(mixer, "SFX");
            Expose(mixer, music, Sfx.MusicParam);
            Expose(mixer, sfx, Sfx.SfxParam);
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            return mixer;
        }

        static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
        {
            foreach (var g in mixer.FindMatchingGroups("Master/" + name)) return g;
            return null;
        }

        static AudioMixerGroup NewGroup(AudioMixer mixer, string name)
        {
            var group = Call(mixer, "CreateNewGroup", name, true);
            Call(mixer, "AddChildToParent", group, ControllerType.GetProperty("masterGroup", Any).GetValue(mixer));
            return (AudioMixerGroup)group;
        }

        static void Expose(AudioMixer mixer, AudioMixerGroup group, string parameter)
        {
            if (mixer.GetFloat(parameter, out _)) return; // already exposed
            var guid = Call(group, "GetGUIDForVolume");
            var prop = ControllerType.GetProperty("exposedParameters", Any);
            var current = (System.Array)prop.GetValue(mixer);
            var elementType = prop.PropertyType.GetElementType();
            var param = System.Activator.CreateInstance(elementType);
            elementType.GetField("guid", Any).SetValue(param, guid);
            elementType.GetField("name", Any).SetValue(param, parameter);
            var extended = System.Array.CreateInstance(elementType, current.Length + 1);
            System.Array.Copy(current, extended, current.Length);
            extended.SetValue(param, current.Length);
            prop.SetValue(mixer, extended);
        }

        // ------------------------------------------------------------------ sounds

        static void DefineSounds()
        {
            // Player weapon (2D)
            Add("pulse_fire", 0.55f, 0.95f, 1.1f, 3, 40, 0, Laser + "light_blast_1.wav", Laser + "light_blast_2.wav", Laser + "light_blast_3.wav");
            Add("overload_fire", 0.9f, 0.95f, 1.05f, 3, 40, 0, Laser + "heavy_blast_001.wav", Laser + "heavy_blast_002.wav", Laser + "heavy_blast_003.wav");
            Add("overload_charge", 0.5f, 1f, 1f, 3, 40, 0, Laser + "continuous_beam_1.wav");
            Add("dry_fire", 0.6f, 1f, 1f, 3, 40, 0, Laser + "weapon_handling_26.wav");
            Add("reload", 0.7f, 1f, 1f, 3, 40, 0, Laser + "reloading_001.wav", Laser + "reloading_002.wav", Laser + "reloading_003.wav");
            Add("reload_done", 0.7f, 1f, 1f, 3, 40, 0, Laser + "weapon_handling_12.wav", Laser + "weapon_handling_19.wav");

            // Impacts and damage
            Add("impact_surface", 0.4f, 0.9f, 1.1f, 3, 35, 0, General + "hit_01.ogg", General + "hit_02.ogg");
            Add("impact_hit", 0.6f, 0.95f, 1.05f, 3, 40, 0, General + "hit_03.ogg", General + "hit_04.ogg");
            Add("impact_crit", 0.75f, 1.25f, 1.35f, 3, 40, 0, General + "metal_03.ogg");
            Add("player_hurt", 0.8f, 0.85f, 0.95f, 3, 40, 0, General + "hit_02.ogg");
            Add("interact", 0.4f, 0.95f, 1.05f, 3, 40, 0, General + "switch_01.ogg");

            // Enemies (3D)
            Add("enemy_laser", 0.5f, 0.9f, 1.1f, 5, 60, 0, Laser + "light_blast_6.wav", Laser + "light_blast_7.wav", Laser + "light_blast_8.wav");
            Add("enemy_hit", 0.6f, 0.9f, 1.1f, 3, 35, 0, General + "metal_01.ogg", General + "metal_02.ogg", General + "metal_03.ogg");
            Add("enemy_die", 0.8f, 0.9f, 1.05f, 4, 45, 0, General + "metal_08.ogg", General + "metal_09.ogg");
            Add("stun", 0.7f, 0.95f, 1.05f, 3, 35, 0, SciFi + "weird_01.ogg");
            Add("alert_beep", 0.7f, 1f, 1.1f, 5, 45, 0, Generated + "alert_beep.wav");
            Add("servo", 0.4f, 1f, 1f, 2, 18, 0, Generated + "servo_loop.wav");
            Add("shock_beam", 0.5f, 1f, 1f, 2, 15, 0, Laser + "continuous_beam_3.wav");
            Add("shield_break", 0.9f, 0.9f, 1.05f, 4, 50, 0, General + "glass_01.ogg", General + "glass_02.ogg");

            // Explosions
            Add("explosion", 1f, 0.9f, 1.1f, 8, 90, 0, SciFi + "explosion_01.ogg", SciFi + "explosion_02.ogg");
            Add("explosion_big", 1.2f, 0.7f, 0.85f, 15, 140, 0, General + "explosion.ogg");
            Add("spark", 0.6f, 0.95f, 1.1f, 3, 30, 0, SciFi + "retro_laser_01.ogg");

            // World
            Add("crate_hit", 0.7f, 0.9f, 1.1f, 3, 30, 0, General + "wooden_01.ogg", General + "wooden_02.ogg");
            Add("crate_break", 0.9f, 0.9f, 1.1f, 4, 40, 0, General + "wooden_03.ogg", General + "wooded_box_open.ogg");
            Add("laser_hum", 0.35f, 1.6f, 1.6f, 1.5f, 12, 0, Generated + "hum_loop.wav");
            Add("laser_zap", 0.8f, 0.95f, 1.05f, 3, 35, 0, SciFi + "retro_laser_02.ogg");
            Add("emitter_off", 0.7f, 0.95f, 1.05f, 3, 30, 0, General + "switch_02.ogg");
            Add("siren", 0.8f, 1f, 1f, 10, 70, 5, Generated + "siren_loop.wav");
            Add("crackle", 0.5f, 1f, 1f, 3, 25, 0, Generated + "crackle_loop.wav");
            Add("water_zap", 0.6f, 0.95f, 1.05f, 3, 30, 0, SciFi + "teleport_01.ogg");
            Add("door_open", 0.8f, 0.95f, 1.05f, 4, 35, 0, General + "door_open.ogg", General + "door_01.ogg");
            Add("door_close", 0.8f, 0.95f, 1.05f, 4, 35, 0, General + "door_close_01.ogg", General + "door_close_02.ogg", General + "door_close_03.ogg");
            Add("power_on", 0.9f, 1f, 1f, 6, 50, 0, General + "machine_01.ogg");
            Add("hum", 0.5f, 1f, 1f, 2, 25, 0, Generated + "hum_loop.wav");
            Add("pickup_medkit", 0.8f, 1f, 1f, 3, 30, 0, General + "plop_01.ogg");
            Add("pickup_battery", 0.8f, 1f, 1f, 3, 30, 0, SciFi + "misc_03.ogg");
            Add("pickup_fuse", 0.8f, 1f, 1f, 3, 30, 0, SciFi + "teleport_02.ogg");
            Add("pickup_pistol", 0.8f, 1f, 1f, 3, 30, 0, SciFi + "misc_05.ogg");
            Add("keypad_key", 0.6f, 0.95f, 1.05f, 3, 30, 0, SciFi + "terminal_01.ogg", SciFi + "terminal_02.ogg", SciFi + "terminal_03.ogg");
            Add("keypad_ok", 0.8f, 1f, 1f, 3, 30, 0, SciFi + "terminal_05.ogg");
            Add("keypad_bad", 0.8f, 1f, 1f, 3, 30, 0, SciFi + "retro_beep_03.ogg");
            Add("radio_static", 0.5f, 0.95f, 1.05f, 3, 30, 0, Generated + "radio_static.wav");
            Add("ui_click", 0.7f, 1f, 1f, 3, 30, 0, Generated + "ui_click.wav");

            // Boss
            Add("orb_fire", 0.8f, 0.7f, 0.8f, 5, 60, 0, Laser + "heavy_blast_006.wav", Laser + "heavy_blast_007.wav");
            Add("stomp_charge", 1f, 1f, 1f, 20, 120, 0, Generated + "stomp_charge.wav");
            Add("stomp", 1.2f, 0.6f, 0.7f, 20, 130, 0, General + "slam_06.ogg", General + "slam_07.ogg");
            Add("titan_step", 1f, 0.9f, 1.1f, 15, 110, 0, Generated + "titan_step.wav");

            // Footsteps
            Add("step_dirt", 0.7f, 0.9f, 1.1f, 2, 25, 0, Steps + "gravel.ogg");
            Add("step_grass", 0.6f, 0.9f, 1.1f, 2, 25, 0, Steps + "leaves01.ogg", Steps + "leaves02.ogg");
            Add("step_sand", 0.7f, 0.85f, 0.95f, 2, 25, 0, Steps + "mud02.ogg");
            Add("step_rock", 0.7f, 0.75f, 0.85f, 2, 25, 0, Steps + "stone01.ogg");
            Add("step_puddle", 0.8f, 0.9f, 1.1f, 2, 25, 0, General + "splash_01.ogg", General + "splash_02.ogg");
            Add("step_stone", 0.7f, 0.9f, 1.1f, 2, 25, 0, Steps + "stone01.ogg");
        }

        static void Add(string id, float volume, float pitchMin, float pitchMax, float minDistance, float maxDistance,
            float playSeconds, params string[] paths)
        {
            var clips = new List<AudioClip>();
            foreach (var path in paths)
            {
                var clip = Load(path);
                if (clip != null) clips.Add(clip);
            }
            if (clips.Count == 0)
            {
                EditorCommandBridge.Log($"audio: no clips found for '{id}'");
                return;
            }
            entries.Add(new SoundEntry
            {
                id = id,
                clips = clips.ToArray(),
                volume = volume,
                pitch = new Vector2(pitchMin, pitchMax),
                minDistance = minDistance,
                maxDistance = maxDistance,
                playSeconds = playSeconds
            });
        }

        static AudioClip Load(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) EditorCommandBridge.Log($"audio: missing clip {path}");
            return clip;
        }
    }
}
