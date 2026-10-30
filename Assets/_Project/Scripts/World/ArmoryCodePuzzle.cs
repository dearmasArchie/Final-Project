using TMPro;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// The maintenance bunker's clues for the armory code (random each run, from GameManager.ArmoryPuzzle):
    /// the wall message gives the color order, each colored locker shows one digit, decoy numbers mislead,
    /// and the hidden locker's digit is glow paint that only shows with the lights off.
    /// </summary>
    public class ArmoryCodePuzzle : MonoBehaviour
    {
        [SerializeField] TMP_Text wallMessage;
        [Tooltip("Locker stencils in color order: Blue, Red, Green, Yellow.")]
        [SerializeField] TMP_Text[] lockerDigits;
        [Tooltip("Glow-paint digits on the same lockers, same order. Only the hidden one is ever shown.")]
        [SerializeField] TMP_Text[] glowDigits;
        [SerializeField] TMP_Text[] decoys;
        [SerializeField] Color dimmedColor = new Color(0.12f, 0.12f, 0.12f);

        TMP_Text[] normalTexts;
        Color[] normalColors;

        void Start()
        {
            var puzzle = GameManager.Instance.ArmoryPuzzle;
            var names = GameManager.ArmoryPuzzleData.ColorNames;

            wallMessage.text =
                "ARMORY CODE\n" +
                $"{names[puzzle.colorOrder[0]]} - {names[puzzle.colorOrder[1]]} - {names[puzzle.colorOrder[2]]} - {names[puzzle.colorOrder[3]]}\n" +
                "<size=60%>some things only show in the dark</size>";

            for (int color = 0; color < 4; color++)
            {
                bool hidden = color == puzzle.hiddenColor;
                lockerDigits[color].text = hidden ? "#" : puzzle.digitByColor[color].ToString();
                glowDigits[color].text = puzzle.digitByColor[color].ToString();
                glowDigits[color].gameObject.SetActive(false);
            }

            string[] decoyFormats = { "BAY {0}", "SHELF {0}", "UNIT {0}" };
            for (int i = 0; i < decoys.Length && i < puzzle.decoys.Length; i++)
                decoys[i].text = string.Format(decoyFormats[i % decoyFormats.Length], puzzle.decoys[i]);

            normalTexts = new TMP_Text[lockerDigits.Length + decoys.Length + 1];
            lockerDigits.CopyTo(normalTexts, 0);
            decoys.CopyTo(normalTexts, lockerDigits.Length);
            normalTexts[normalTexts.Length - 1] = wallMessage;
            normalColors = new Color[normalTexts.Length];
            for (int i = 0; i < normalTexts.Length; i++) normalColors[i] = normalTexts[i].color;
        }

        /// <summary>Called by the bunker's LightSwitch.</summary>
        public void SetLights(bool on)
        {
            // Painted stencils are hard to read in the dark; only the glow paint stands out.
            for (int i = 0; i < normalTexts.Length; i++) normalTexts[i].color = on ? normalColors[i] : dimmedColor;
            int hidden = GameManager.Instance.ArmoryPuzzle.hiddenColor;
            glowDigits[hidden].gameObject.SetActive(!on);
        }
    }
}
