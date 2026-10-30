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
                float goal = i == active && target != null ? 1f : 0f;
                s.volume = Mathf.MoveTowards(s.volume, goal, Time.unscaledDeltaTime / fade);
                if (goal == 0f && s.volume == 0f && s.isPlaying) s.Stop();
            }
        }

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
