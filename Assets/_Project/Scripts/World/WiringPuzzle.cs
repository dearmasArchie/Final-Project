using System.Collections.Generic;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Hub junction box: a 3×3 grid of wire tiles (straight or corner). Rotate tiles with E until power flows
    /// from the IN side to the OUT side; that restores the elevator's control circuit. The path and the
    /// scramble are random each time the scene loads, and the grid is always solvable.
    /// </summary>
    public class WiringPuzzle : MonoBehaviour
    {
        public const string SolvedId = "ElevatorCircuitRestored";
        public const int Size = 3;

        // Sides: 0 = North (up), 1 = East (screen right), 2 = South, 3 = West (screen left)
        static readonly int[] RowStep = { -1, 0, 1, 0 };
        static readonly int[] ColStep = { 0, 1, 0, -1 };

        [Tooltip("Tiles in reading order: row 0 left→right, then row 1, then row 2.")]
        [SerializeField] WireTile[] tiles;
        [SerializeField] Transform inputMarker;
        [SerializeField] Transform outputMarker;
        [SerializeField] float tileSpacing = 0.65f;

        int inputRow, outputRow;
        int[] solvedRotation;

        public bool Solved { get; private set; }
        public int InputRow => inputRow;
        public int OutputRow => outputRow;

        void Start()
        {
            for (int i = 0; i < tiles.Length; i++) tiles[i].Init(this);
            Generate();
            if (GameManager.Instance.IsCleared(SolvedId)) SolveNow(silent: true);
            else Evaluate(announce: false);
        }

        // ------------------------------------------------------------------ generation

        void Generate()
        {
            List<Vector2Int> path;
            do
            {
                inputRow = Random.Range(0, Size);
                path = RandomPath(new Vector2Int(inputRow, 0));
            } while (path == null || path.Count < 4); // avoid trivially short routes
            outputRow = path[path.Count - 1].x;

            solvedRotation = new int[Size * Size];
            var onPath = new bool[Size * Size];
            for (int i = 0; i < path.Count; i++)
            {
                var cell = path[i];
                int entry = i == 0 ? 3 : SideToward(cell, path[i - 1]);
                int exit = i == path.Count - 1 ? 1 : SideToward(cell, path[i + 1]);
                bool straight = (entry + 2) % 4 == exit;
                int index = cell.x * Size + cell.y;
                onPath[index] = true;
                tiles[index].SetShape(straight);
                solvedRotation[index] = RotationFor(straight, entry, exit);
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                if (onPath[i]) continue;
                tiles[i].SetShape(Random.value < 0.5f);
                solvedRotation[i] = Random.Range(0, 4);
            }

            // Scramble, making sure it doesn't start solved
            do
            {
                foreach (var tile in tiles) tile.SetRotation(Random.Range(0, 4));
            } while (Trace(null));

            PlaceMarkers();
        }

        /// <summary>Random self-avoiding walk from the left column to any cell in the right column.</summary>
        static List<Vector2Int> RandomPath(Vector2Int start)
        {
            var path = new List<Vector2Int> { start };
            var visited = new HashSet<Vector2Int> { start };
            return Extend(path, visited) ? path : null;
        }

        static bool Extend(List<Vector2Int> path, HashSet<Vector2Int> visited)
        {
            var current = path[path.Count - 1];
            if (current.y == Size - 1 && path.Count >= 3 && Random.value < 0.6f) return true;

            var sides = new List<int> { 0, 1, 2, 3 };
            for (int i = sides.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (sides[i], sides[j]) = (sides[j], sides[i]);
            }
            foreach (int side in sides)
            {
                var next = new Vector2Int(current.x + RowStep[side], current.y + ColStep[side]);
                if (next.x < 0 || next.x >= Size || next.y < 0 || next.y >= Size || visited.Contains(next)) continue;
                path.Add(next);
                visited.Add(next);
                if (Extend(path, visited)) return true;
                path.RemoveAt(path.Count - 1);
                visited.Remove(next);
            }
            return current.y == Size - 1;
        }

        static int SideToward(Vector2Int from, Vector2Int to)
        {
            for (int side = 0; side < 4; side++)
                if (from.x + RowStep[side] == to.x && from.y + ColStep[side] == to.y) return side;
            return -1;
        }

        static int RotationFor(bool straight, int a, int b)
        {
            for (int rotation = 0; rotation < 4; rotation++)
            {
                var open = WireTile.Openings(straight, rotation);
                if (open.Contains(a) && open.Contains(b)) return rotation;
            }
            return 0;
        }

        void PlaceMarkers()
        {
            // Row 0 is the top row. Tile (row, col) local position: x = -col * spacing (screen right is -x), y = -row * spacing.
            if (inputMarker != null) inputMarker.localPosition = new Vector3(tileSpacing, -inputRow * tileSpacing, 0f);
            if (outputMarker != null) outputMarker.localPosition = new Vector3(-Size * tileSpacing, -outputRow * tileSpacing, 0f);
        }

        // ------------------------------------------------------------------ evaluation

        /// <summary>Called by a tile after it rotates.</summary>
        public void OnTileRotated()
        {
            if (!Solved) Evaluate(announce: true);
        }

        void Evaluate(bool announce)
        {
            var lit = new HashSet<WireTile>();
            bool solved = Trace(lit);
            foreach (var tile in tiles) tile.SetLit(lit.Contains(tile));
            if (!solved) return;

            Solved = true;
            GameManager.Instance.MarkCleared(SolvedId);
            if (!announce) return;
            if (HUDController.Instance != null) HUDController.Instance.ShowToast("ELEVATOR CIRCUIT RESTORED");
            Radio.Say("That did it – the elevator control circuit is live. It'll take you back up once you have both fuses.");
            ObjectiveManager.RefreshNow();
        }

        /// <summary>Follows the wire from the IN side. Returns true if it reaches the OUT side; fills 'lit' with powered tiles.</summary>
        bool Trace(HashSet<WireTile> lit)
        {
            int row = inputRow, col = 0, entry = 3;
            for (int steps = 0; steps < Size * Size + 1; steps++)
            {
                var tile = tiles[row * Size + col];
                var open = tile.CurrentOpenings;
                if (!open.Contains(entry)) return false;
                lit?.Add(tile);
                int exit = open[0] == entry ? open[1] : open[0];
                int nextRow = row + RowStep[exit], nextCol = col + ColStep[exit];
                if (nextRow < 0 || nextRow >= Size || nextCol < 0 || nextCol >= Size)
                    return exit == 1 && col == Size - 1 && row == outputRow;
                row = nextRow;
                col = nextCol;
                entry = (exit + 2) % 4;
            }
            return false;
        }

        void SolveNow(bool silent)
        {
            for (int i = 0; i < tiles.Length; i++) tiles[i].SetRotation(solvedRotation[i]);
            Evaluate(announce: !silent);
        }

        /// <summary>Automated tests only: rotate every tile into the solved position.</summary>
        public void SolveForTest() => SolveNow(silent: false);
    }
}
