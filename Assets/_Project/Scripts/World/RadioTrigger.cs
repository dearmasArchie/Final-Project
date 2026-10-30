using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Plays a radio line once per run, either when the player enters this trigger volume
    /// or when Play() is called from an event (e.g. a pickup's onPickedUp).
    /// </summary>
    public class RadioTrigger : MonoBehaviour
    {
        [SerializeField, TextArea(2, 4)] string line;
        [SerializeField] float seconds = 6f;
        [SerializeField] string persistentId;
        [Tooltip("Only play once both Fuse 1 and Fuse 2 are held (return-trip lines).")]
        [SerializeField] bool requireFusesOneAndTwo;
        [Tooltip("Only play while this ID is NOT yet cleared (e.g. the 'no power' line before the generator is switched on).")]
        [SerializeField] string onlyIfNotCleared;

        void Awake()
        {
            if (TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
                if (!TryGetComponent<Rigidbody>(out var body)) body = gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null) Play();
        }

        public void Play()
        {
            var gm = GameManager.Instance;
            if (requireFusesOneAndTwo && !(gm.HasFuse(1) && gm.HasFuse(2))) return;
            if (!string.IsNullOrEmpty(onlyIfNotCleared) && gm.IsCleared(onlyIfNotCleared)) return;
            if (!string.IsNullOrEmpty(persistentId))
            {
                if (gm.IsCleared(persistentId)) return;
                gm.MarkCleared(persistentId);
            }
            Radio.Say(line, seconds);
            ObjectiveManager.RefreshNow();
        }
    }
}
