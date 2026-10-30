using UnityEngine;

namespace SignalLost
{
    /// <summary>Carries HP and ammo across scene loads through GameManager.</summary>
    [RequireComponent(typeof(PlayerHealth), typeof(ArcPistol))]
    public class PlayerPersistence : MonoBehaviour
    {
        PlayerHealth health;
        ArcPistol pistol;

        void Awake()
        {
            health = GetComponent<PlayerHealth>();
            pistol = GetComponent<ArcPistol>();
        }

        void OnEnable() => SceneLoader.BeforeSceneUnload += Save;
        void OnDisable() => SceneLoader.BeforeSceneUnload -= Save;

        void Start()
        {
            var gm = GameManager.Instance;
            if (!gm.HasPlayerState) return;
            health.SetHealth(gm.PlayerHealth);
            pistol.SetAmmo(gm.PlayerMagazine, gm.PlayerReserveAmmo);
        }

        void Save() => GameManager.Instance.SavePlayerState(health.Current, pistol.Magazine, pistol.Reserve);
    }
}
