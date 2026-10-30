using UnityEngine;
using UnityEngine.UI;

namespace SignalLost
{
    /// <summary>Mouse sensitivity and volume sliders, saved in PlayerPrefs. Used by the main and pause menus.</summary>
    public class SettingsPanel : MonoBehaviour
    {
        public const string MasterVolumeKey = "MasterVolume";
        public const string MusicVolumeKey = "MusicVolume";
        public const string SfxVolumeKey = "SfxVolume";

        [SerializeField] Slider sensitivity;
        [SerializeField] Slider masterVolume;
        [SerializeField] Slider musicVolume;
        [SerializeField] Slider sfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplySaved() => AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);

        void OnEnable()
        {
            Bind(sensitivity, FirstPersonController.SensitivityKey, 1f);
            Bind(masterVolume, MasterVolumeKey, 1f);
            Bind(musicVolume, MusicVolumeKey, 0.8f);
            Bind(sfxVolume, SfxVolumeKey, 1f);
        }

        static void Bind(Slider slider, string key, float defaultValue)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.SetValueWithoutNotify(PlayerPrefs.GetFloat(key, defaultValue));
            slider.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat(key, value);
                if (key == MasterVolumeKey) AudioListener.volume = value;
            });
        }
    }
}
