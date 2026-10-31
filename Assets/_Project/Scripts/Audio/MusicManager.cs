using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Plays one music track at a time and crossfades between them: exploration by default, combat while any bot
    /// is chasing or attacking, and boss music while the player is near a living Titan.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        enum Mood { Exploration, Combat, Boss }

        const float CombatHold = 5f;
        const float BossRange = 70f;

        static MusicManager instance;

        AudioSource victorySource;
        bool victoryPlaying;

        /// <summary>The winning song: everything else fades out and the victory track plays, even while the game is paused.</summary>
        public static void PlayVictory()
        {
            if (instance == null) return;
            instance.StartVictory();
        }

        /// <summary>Back to normal music (called when leaving the victory screen).</summary>
        public static void StopVictory()
        {
            if (instance != null) instance.EndVictory();
        }

        void StartVictory()
        {
            var library = Sfx.Library;
            if (library == null || library.victory == null) return;
            victoryPlaying = true;
            foreach (var s in sources) { s.Stop(); s.volume = 0f; }
            if (victorySource == null)
            {
                victorySource = gameObject.AddComponent<AudioSource>();
                victorySource.outputAudioMixerGroup = library.musicGroup;
                victorySource.loop = true;
                victorySource.playOnAwake = false;
                victorySource.spatialBlend = 0f;
                victorySource.ignoreListenerPause = true; // the victory screen pauses the game
            }
            victorySource.clip = library.victory;
            victorySource.volume = library.victoryGain;
            victorySource.Play();
        }

        void EndVictory()
        {
            victoryPlaying = false;
            if (victorySource != null) victorySource.Stop();
            target = null; // lets the normal music pick a track again
        }

        public static bool VictoryPlaying => instance != null && instance.victoryPlaying && instance.victorySource != null && instance.victorySource.isPlaying;

        AudioSource[] sources;
        int active;
        AudioClip target;
        float fade = 2f;
        float lastCombatTime = -100f;
        float nextBossCheck;
        BossController boss;
        Transform player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Reset() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null || Sfx.Library == null) return;
            var go = new GameObject("MusicManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MusicManager>();
        }

        void Awake()
        {
            var library = Sfx.Library;
            sources = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.outputAudioMixerGroup = library.musicGroup;
                s.loop = true;
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.volume = 0f;
                sources[i] = s;
            }
        }

        void Update()
        {
            if (victoryPlaying) return;
            var library = Sfx.Library;
            Mood mood = CurrentMood();
            AudioClip wanted = mood == Mood.Boss ? library.boss : mood == Mood.Combat ? library.combat : library.exploration;
            if (wanted == null) wanted = library.exploration;

            if (wanted != target)
            {
                target = wanted;
                fade = mood == Mood.Combat ? 1.2f : mood == Mood.Boss ? 2f : 4f; // snap into combat, ease out of it
                StartTrack(target);
            }

            // Move each source toward its goal volume; the old track fades out and is stopped when silent.
            for (int i = 0; i < sources.Length; i++)
            {
                var s = sources[i];
                float goal = i == active && target != null ? TrackGain(library, target) : 0f;
                s.volume = Mathf.MoveTowards(s.volume, goal, Time.unscaledDeltaTime / fade);
                if (goal == 0f && s.volume == 0f && s.isPlaying) s.Stop();
            }
        }

        static float TrackGain(AudioLibrary library, AudioClip clip) =>
            clip == library.boss ? library.bossGain : clip == library.combat ? library.combatGain : library.explorationGain;

        void StartTrack(AudioClip clip)
        {
            if (clip == null) return;
            active = 1 - active;
            var s = sources[active];
            s.clip = clip;
            s.volume = 0f;
            s.Play();
        }

        Mood CurrentMood()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }

            if (Time.unscaledTime >= nextBossCheck)
            {
                nextBossCheck = Time.unscaledTime + 1f;
                if (boss == null) boss = FindFirstObjectByType<BossController>();
            }
            if (boss != null && player != null && !boss.Health.IsDead
                && Vector3.Distance(boss.transform.position, player.position) <= BossRange)
                return Mood.Boss;

            foreach (var enemy in EnemyAI.All)
            {
                var state = enemy.CurrentState;
                if (state == EnemyAI.State.Chase || state == EnemyAI.State.Attack) lastCombatTime = Time.unscaledTime;
            }
            return Time.unscaledTime - lastCombatTime < CombatHold ? Mood.Combat : Mood.Exploration;
        }
    }
}
