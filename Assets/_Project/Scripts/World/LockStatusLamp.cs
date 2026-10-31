using TMPro;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Status light and display on a keypad lock: red LOCKED while its door is locked, green UNLOCKED once it opens up.
    /// Reads Door.IsLocked, so any script that locks or unlocks the door is reflected on the keypad.
    /// </summary>
    public class LockStatusLamp : MonoBehaviour
    {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly Color Red = new Color(1f, 0.12f, 0.08f);
        static readonly Color Green = new Color(0.25f, 1f, 0.4f);

        [SerializeField] Door door;
        [SerializeField] Renderer lamp;
        [SerializeField] TMP_Text label;

        MaterialPropertyBlock block;
        bool? shownLocked;

        void Update()
        {
            bool locked = door != null && door.IsLocked;
            if (shownLocked == locked) return;
            shownLocked = locked;
            Color color = locked ? Red : Green;
            if (lamp != null)
            {
                block ??= new MaterialPropertyBlock();
                lamp.GetPropertyBlock(block);
                block.SetColor(EmissionColor, color * 3f);
                lamp.SetPropertyBlock(block);
            }
            if (label != null)
            {
                label.text = locked ? "LOCKED" : "UNLOCKED";
                label.color = color;
            }
        }
    }
}
