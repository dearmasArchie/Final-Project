using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Trigger volume that saves a checkpoint when the player walks in (once per stage of progress).
    /// The SpawnPoint on the same object is where "Retry from Checkpoint" puts the player.
    /// </summary>
    [RequireComponent(typeof(SpawnPoint), typeof(BoxCollider))]
    public class Checkpoint : MonoBehaviour
    {
        public enum Condition { Always, HasFusesOneAndTwo, UploadComplete }

        [SerializeField] Condition condition = Condition.Always;

        SpawnPoint spawn;

        void Awake()
        {
            spawn = GetComponent<SpawnPoint>();
            GetComponent<BoxCollider>().isTrigger = true;
            if (!TryGetComponent<Rigidbody>(out var body)) body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead || !ConditionMet()) return;

            var gm = GameManager.Instance;
            // Save again here only if progress has changed since the last save at this checkpoint.
            string key = $"{spawn.Id}:{gm.FusesCollected}:{gm.IsCleared(WaveSpawner.CompletedId)}";
            if (gm.LastCheckpointKey == key) return;

            var pistol = health.GetComponent<ArcPistol>();
            gm.SaveCheckpoint(key, gameObject.scene.name, spawn.Id, health.Current, pistol.Magazine, pistol.Reserve);
            if (HUDController.Instance != null) HUDController.Instance.ShowToast("CHECKPOINT REACHED");
        }

        bool ConditionMet()
        {
            var gm = GameManager.Instance;
            switch (condition)
            {
                case Condition.HasFusesOneAndTwo: return gm.HasFuse(1) && gm.HasFuse(2);
                case Condition.UploadComplete: return gm.IsCleared(WaveSpawner.CompletedId);
                default: return true;
            }
        }
    }
}
