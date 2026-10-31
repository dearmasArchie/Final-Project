using System.Text;
using TMPro;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// The maintenance bunker's clues for the armory code (random each run, from GameManager.ArmoryPuzzle):
    /// the wall message gives the color order, each of the four code colors is a painted smudge on a wall with one
    /// digit, many decoy smudges in other colors carry random numbers, and the hidden digit is glow paint that only
    /// shows with the lights off. The code itself is always the four digits read in the wall message's color order.
    /// </summary>
    public class ArmoryCodePuzzle : MonoBehaviour
    {
        [SerializeField] TMP_Text wallMessage;
        [Tooltip("Painted digits in color order: Blue, Red, Green, Yellow.")]
        [SerializeField] TMP_Text[] lockerDigits;
        [Tooltip("Glow-paint digits beside the painted ones, same order. Only the hidden one is ever shown.")]
        [SerializeField] TMP_Text[] glowDigits;
        [Tooltip("Decoy paint numbers in colors that are not part of the code. Their text is random each run.")]
        [SerializeField] TMP_Text[] decoys;
        [Tooltip("Paint smudge quads behind the digits and decoys; darkened with the lights off.")]
        [SerializeField] Renderer[] smudges;
        [SerializeField] Color dimmedColor = new Color(0.12f, 0.12f, 0.12f);
        [SerializeField] float darkSmudgeBrightness = 0.15f;

        TMP_Text[] normalTexts;
        Color[] normalColors;
        Material[] smudgeMaterials;
        Color[] smudgeColors;

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
                lockerDigits[color].text = Hand(hidden ? "#" : puzzle.digitByColor[color].ToString(), color * 31 + 5);
                glowDigits[color].text = Hand(puzzle.digitByColor[color].ToString(), color * 17 + 3);
                glowDigits[color].gameObject.SetActive(false);
            }

            // Decoys: random numbers of one to three digits (and one of four) that look as painted as the real ones
            for (int i = 0; i < decoys.Length; i++)
            {
                int digits = i == 0 ? 4 : Random.value < 0.35f ? 1 : Random.value < 0.6f ? 2 : 3;
                var number = new StringBuilder();
                for (int d = 0; d < digits; d++) number.Append(Random.Range(d == 0 ? 1 : 0, 10));
                decoys[i].text = Hand(number.ToString(), i * 13 + 7);
            }

            normalTexts = new TMP_Text[lockerDigits.Length + decoys.Length + 1];
            lockerDigits.CopyTo(normalTexts, 0);
            decoys.CopyTo(normalTexts, lockerDigits.Length);
            normalTexts[normalTexts.Length - 1] = wallMessage;
            normalColors = new Color[normalTexts.Length];
            for (int i = 0; i < normalTexts.Length; i++) normalColors[i] = normalTexts[i].color;

            smudgeMaterials = new Material[smudges.Length];
            smudgeColors = new Color[smudges.Length];
            for (int i = 0; i < smudges.Length; i++)
            {
                smudgeMaterials[i] = smudges[i].material;      // per-renderer instance, so the shared asset stays untouched
                smudgeColors[i] = smudgeMaterials[i].GetColor("_BaseColor");
            }
        }

        /// <summary>Called by the bunker's LightSwitch.</summary>
        public void SetLights(bool on)
        {
            // Painted stencils are hard to read in the dark; only the glow paint stands out.
            for (int i = 0; i < normalTexts.Length; i++) normalTexts[i].color = on ? normalColors[i] : dimmedColor;
            for (int i = 0; i < smudgeMaterials.Length; i++)
            {
                var c = smudgeColors[i];
                smudgeMaterials[i].SetColor("_BaseColor", on ? c : new Color(c.r * darkSmudgeBrightness, c.g * darkSmudgeBrightness, c.b * darkSmudgeBrightness, c.a));
            }
            int hidden = GameManager.Instance.ArmoryPuzzle.hiddenColor;
            glowDigits[hidden].gameObject.SetActive(!on);
        }

        /// <summary>
        /// Gives each character a hand-painted look with rich text: its own tilt, a little vertical drift and width,
        /// seeded so a given digit looks the same every time it is shown.
        /// </summary>
        static string Hand(string text, int seed)
        {
            var rng = new System.Random(seed);
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                int tilt = rng.Next(-14, 15);
                float drift = (rng.Next(-6, 7)) / 100f;
                int spacing = rng.Next(-3, 6);
                sb.Append($"<i><rotate={tilt}><voffset={drift:0.00}em><cspace={spacing / 10f:0.0}em>{c}</cspace></voffset></rotate></i>");
            }
            return sb.ToString();
        }
    }
}
