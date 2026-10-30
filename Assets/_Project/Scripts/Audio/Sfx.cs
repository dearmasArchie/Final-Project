using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace SignalLost
{
    /// <summary>
    /// Sound effects by name: <c>Sfx.Play("explosion", position)</c> (3D), <c>Sfx.Play2D("ui_click")</c>, and
    /// <c>Sfx.Loop(gameObject, "hum")</c> (3D loop on an object). Everything goes through the SFX mixer group.
    /// Missing ids or a missing library are ignored, so gameplay never depends on audio.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public const string MusicParam = "MusicVolume";
        public const string SfxParam = "SfxVolume";
        const int PoolSize = 24;

        static Sfx instance;
        static AudioLibrary library;
        static bool libraryLoaded;

        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<AudioSource, float> stopTimes = new Dictionary<AudioSource, float>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        public static AudioLibrary Library
        {
            get
            {
                if (!libraryLoaded)
                {
                    library = Resources.Load<AudioLibrary>("AudioLibrary");
                    libraryLoaded = true;
                }
                return library;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Reset()
        {
            instance = null;
            library = null;
            libraryLoaded = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Ensure();
            ApplySavedVolumes();
        }

        static Sfx Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("SfxPlayer");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Sfx>();
            return instance;
        }

        // ------------------------------------------------------------------ volumes

        /// <summary>Slider value 0..1 to decibels on the mixer's Music or SFX group.</summary>
        public static void SetMixerVolume(string param, float linear)
        {
            var mixer = Library != null ? Library.mixer : null;
            if (mixer == null) return;
            mixer.SetFloat(param, linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f);
        }

        public static void ApplySavedVolumes()
        {
            SetMixerVolume(MusicParam, PlayerPrefs.GetFloat(SettingsPanel.MusicVolumeKey, 0.8f));
            SetMixerVolume(SfxParam, PlayerPrefs.GetFloat(SettingsPanel.SfxVolumeKey, 1f));
        }

        // ------------------------------------------------------------------ one-shots

        /// <summary>3D one-shot at a world position.</summary>
        public static void Play(string id, Vector3 position) => Ensure().PlayInternal(id, position, true, false);

        /// <summary>3D one-shot with its library volume multiplied (softer walking steps, louder sprinting).</summary>
        public static void PlayScaled(string id, Vector3 position, float volumeScale) =>
            Ensure().PlayInternal(id, position, true, false, volumeScale);

        /// <summary>2D one-shot (player-centric sounds, UI).</summary>
        public static void Play2D(string id) => Ensure().PlayInternal(id, Vector3.zero, false, false);

        /// <summary>2D one-shot that still plays while the game is paused (menu clicks).</summary>
        public static void PlayUi(string id) => Ensure().PlayInternal(id, Vector3.zero, false, true);

        void PlayInternal(string id, Vector3 position, bool spatial, bool ignorePause, float volumeScale = 1f)
        {
            var entry = Entry(id);
            if (entry == null) return;

            // Same sound within 40 ms is one sound (a shotgun of bolt hits, double triggers).
            if (lastPlayed.TryGetValue(id, out float last) && Time.unscaledTime - last < 0.04f) return;
            lastPlayed[id] = Time.unscaledTime;

            var source = FreeSource();
            if (source == null) return;
            Configure(source, entry, spatial);
            source.volume = entry.volume * volumeScale;
            source.ignoreListenerPause = ignorePause;
            source.transform.position = position;
            source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
            source.loop = entry.playSeconds > 0f;
            source.Play();
            if (source.loop) stopTimes[source] = Time.time + entry.playSeconds;
        }

        AudioSource FreeSource()
        {
            foreach (var s in pool) if (!s.isPlaying) return s;
            if (pool.Count >= PoolSize) return null;
            var go = new GameObject("Sfx" + pool.Count);
            go.transform.SetParent(transform);
            var created = go.AddComponent<AudioSource>();
            created.playOnAwake = false;
            pool.Add(created);
            return created;
        }

        void Update()
        {
            if (stopTimes.Count == 0) return;
            List<AudioSource> done = null;
            foreach (var pair in stopTimes)
                if (Time.time >= pair.Value) (done ??= new List<AudioSource>()).Add(pair.Key);
            if (done == null) return;
            foreach (var s in done)
            {
                s.Stop();
                s.loop = false;
                stopTimes.Remove(s);
            }
        }

        // ------------------------------------------------------------------ loops

        /// <summary>Adds a 3D looping source to an object. Pass autoplay false to start/stop it yourself.</summary>
        public static AudioSource Loop(GameObject target, string id, bool autoplay = true, float volumeScale = 1f, float pitch = 1f)
        {
            var entry = Entry(id);
            if (entry == null) return null;
            var source = target.AddComponent<AudioSource>();
            Configure(source, entry, true);
            source.clip = entry.clips[0];
            source.loop = true;
            source.volume = entry.volume * volumeScale;
            source.pitch = pitch;
            source.time = Random.value * source.clip.length; // several hums on screen shouldn't phase together
            if (autoplay) source.Play();
            return source;
        }

        static SoundEntry Entry(string id)
        {
            var lib = Library;
            if (lib == null) return null;
            var entry = lib.Find(id);
            return entry != null && entry.clips != null && entry.clips.Length > 0 ? entry : null;
        }

        static void Configure(AudioSource source, SoundEntry entry, bool spatial)
        {
            source.outputAudioMixerGroup = Library.sfxGroup;
            source.volume = entry.volume;
            source.pitch = Random.Range(entry.pitch.x, entry.pitch.y);
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = entry.minDistance;
            source.maxDistance = entry.maxDistance;
            source.dopplerLevel = 0f;
            source.ignoreListenerPause = false;
            source.playOnAwake = false;
        }
    }
}
