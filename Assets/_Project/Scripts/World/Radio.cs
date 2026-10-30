using UnityEngine;

namespace SignalLost
{
    /// <summary>Dr. Reyes over the radio: shows a subtitle in the HUD's radio box.</summary>
    public static class Radio
    {
        public static void Say(string line, float seconds = 6f)
        {
            Sfx.Play2D("radio_static");
            if (HUDController.Instance != null) HUDController.Instance.ShowRadio("DR. REYES", line, seconds);
        }
    }
}
