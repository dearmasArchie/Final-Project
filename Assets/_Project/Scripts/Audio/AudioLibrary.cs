using System;
using UnityEngine;
using UnityEngine.Audio;

namespace SignalLost
{
    [Serializable]
    public class SoundEntry
    {
        public string id;
        public AudioClip[] clips;
        [Range(0f, 2f)] public float volume = 1f;
        public Vector2 pitch = new Vector2(0.95f, 1.05f);
        public float minDistance = 3f;
        public float maxDistance = 40f;
        [Tooltip("0 = play the clip once. Above 0 = loop the clip for this many seconds (sirens).")]
        public float playSeconds;
    }

    /// <summary>
    /// Every sound the game plays, by name. Built by the <c>audio</c> editor command (AudioBuilder) and loaded from
    /// Resources. Swap clips here in the Inspector; scripts only know the ids.
    /// </summary>
    public class AudioLibrary : ScriptableObject
    {
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioClip exploration;
        public AudioClip combat;
        public AudioClip boss;
        [Range(0f, 1f)] public float explorationGain = 1f;
        [Range(0f, 1f)] public float combatGain = 0.35f;   // the combat loop is mastered much louder than the calm one
        [Range(0f, 1f)] public float bossGain = 0.8f;
        public SoundEntry[] sounds;

        public SoundEntry Find(string id)
        {
            foreach (var s in sounds) if (s.id == id) return s;
            return null;
        }
    }
}
