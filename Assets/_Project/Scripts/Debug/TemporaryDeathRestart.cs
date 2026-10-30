using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalLost.DebugTools
{
    /// <summary>
    /// TEMPORARY until the Game Over screen and checkpoints exist (Phase 3): when the player dies,
    /// wait 2 seconds and reload the current scene at full health. Fuses, timer and cleared enemies are kept.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class TemporaryDeathRestart : MonoBehaviour
    {
        void OnEnable() => GetComponent<PlayerHealth>().Died += OnDied;
        void OnDisable() => GetComponent<PlayerHealth>().Died -= OnDied;

        void OnDied()
        {
            foreach (var behaviour in new MonoBehaviour[]
                     { GetComponent<FirstPersonController>(), GetComponent<ArcPistol>(), GetComponent<PlayerInteraction>() })
                behaviour.enabled = false;
            if (HUDController.Instance != null) HUDController.Instance.SetEventText("YOU DIED – restarting...");
            StartCoroutine(Restart());
        }

        IEnumerator Restart()
        {
            yield return new WaitForSeconds(2f);
            int index = SceneManager.GetActiveScene().buildIndex;
            if (index >= 0)
            {
                SceneManager.LoadScene(index);
                yield break;
            }

            // Test scenes aren't in Build Settings and can't be reloaded: just revive in place.
            var health = GetComponent<PlayerHealth>();
            health.SetHealth(health.Max);
            foreach (var behaviour in new MonoBehaviour[]
                     { GetComponent<FirstPersonController>(), GetComponent<ArcPistol>(), GetComponent<PlayerInteraction>() })
                behaviour.enabled = true;
            if (HUDController.Instance != null) HUDController.Instance.SetEventText(null);
        }
    }
}
