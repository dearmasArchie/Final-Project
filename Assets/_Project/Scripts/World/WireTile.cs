using System.Collections.Generic;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// One tile in the junction box grid. Shows a straight or corner wire; E rotates it 90° clockwise.
    /// Glows when power flows through it.
    /// </summary>
    public class WireTile : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform pivot;
        [Tooltip("Wire bars on the pivot: North, East, South, West (screen directions at rotation 0).")]
        [SerializeField] Renderer[] bars;

        static readonly Color LitColor = new Color(0.3f, 0.95f, 1f);
        static readonly Color DarkColor = new Color(0.2f, 0.2f, 0.22f);

        WiringPuzzle puzzle;
        MaterialPropertyBlock block;
        bool straight;
        int rotation;

        public List<int> CurrentOpenings => Openings(straight, rotation);

        public string Prompt => puzzle != null && puzzle.Solved ? null : "Rotate Wire";
        public bool CanInteract => puzzle != null && !puzzle.Solved;

        /// <summary>Open sides after rotating clockwise 'rotation' times (0 = N, 1 = E, 2 = S, 3 = W).</summary>
        public static List<int> Openings(bool straight, int rotation)
        {
            int a = 0, b = straight ? 2 : 1; // straight: N+S, corner: N+E
            return new List<int> { (a + rotation) % 4, (b + rotation) % 4 };
        }

        public void Init(WiringPuzzle owner)
        {
            puzzle = owner;
            block = new MaterialPropertyBlock();
        }

        public void SetShape(bool isStraight)
        {
            straight = isStraight;
            // Bars at rotation 0: straight shows N and S, corner shows N and E.
            bars[0].enabled = true;
            bars[1].enabled = !straight;
            bars[2].enabled = straight;
            bars[3].enabled = false;
        }

        public void SetRotation(int value)
        {
            rotation = ((value % 4) + 4) % 4;
            // Clockwise on screen: the viewer faces the wall, so this is a positive turn about the wall normal.
            pivot.localRotation = Quaternion.Euler(0f, 0f, rotation * 90f);
        }

        public void SetLit(bool lit)
        {
            var color = lit ? LitColor : DarkColor;
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", lit ? color * 3f : Color.black);
            foreach (var bar in bars) bar.SetPropertyBlock(block);
        }

        public void Interact(GameObject interactor)
        {
            SetRotation(rotation + 1);
            puzzle.OnTileRotated();
        }
    }
}
