using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DiceDrive.Board
{
    /// <summary>
    /// The level's single, ordered movement path: position 0 is the start, the last position is the exit.
    /// Each position is a world point (where the player stands) plus the tile cells it covers (for highlights
    /// and for matching enemies to positions).
    ///
    /// Edit the list in the Inspector, or use the context menu "Auto-build From Tilemaps" to generate it from the
    /// walkable tilemaps, then reorder/trim as needed. Positions are labelled in the Scene view.
    /// </summary>
    public sealed class BoardPath : MonoBehaviour
    {
        [Serializable]
        public sealed class PathPoint
        {
            [Tooltip("Where the player stands on this position (world space, before the character's own offset).")]
            public Vector3 position;

            [Tooltip("Tile cells that belong to this position (highlights and enemy matching).")]
            public List<Vector3Int> cells = new List<Vector3Int>();
        }

        [Tooltip("Index 0 = start, last = exit.")]
        [SerializeField] private List<PathPoint> points = new List<PathPoint>();

        [Tooltip("Designed number of playable positions (excluding start). Only used to warn when the path differs.")]
        [SerializeField, Min(1)] private int expectedPlayablePositions = 18;

        [Header("Auto-build")]
        [Tooltip("Walkable tilemaps. Each connected group of painted cells on one tilemap becomes one position.")]
        [SerializeField] private Tilemap[] walkableTilemaps;
        [SerializeField] private string[] walkableTilemapNames = { "Ground", "Parallel_Ground" };
        [Tooltip("Path starts on the position nearest to this (defaults to the object tagged Player).")]
        [SerializeField] private Transform startMarker;

        public int Count => points.Count;
        public int ExitIndex => points.Count - 1;
        public int ExpectedPlayablePositions => expectedPlayablePositions;
        public Tilemap ReferenceTilemap => ResolveTilemaps().FirstOrDefault();

        public Vector3 GetPosition(int index) => points[index].position;
        public IReadOnlyList<Vector3Int> GetCells(int index) => points[index].cells;

        /// <summary>Path index whose cells contain <paramref name="cell"/>, or -1.</summary>
        public int IndexOfCell(Vector3Int cell)
        {
            for (var i = 0; i < points.Count; i++)
            {
                if (points[i].cells.Contains(cell)) return i;
            }

            return -1;
        }

        /// <summary>Path index under a world point (by cell), or -1.</summary>
        public int IndexAt(Vector3 world)
        {
            var map = ReferenceTilemap;
            return map != null ? IndexOfCell(map.WorldToCell(world)) : -1;
        }

        public int NearestIndex(Vector3 world)
        {
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < points.Count; i++)
            {
                var d = (points[i].position - world).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = i; }
            }

            return best;
        }

        private Tilemap[] ResolveTilemaps()
        {
            if (walkableTilemaps != null && walkableTilemaps.Any(t => t != null))
            {
                return walkableTilemaps.Where(t => t != null).ToArray();
            }

            var all = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            return walkableTilemapNames.Select(n => all.FirstOrDefault(t => t.name == n)).Where(t => t != null).ToArray();
        }

        // ------------------------------------------------------------------ auto-build

        private sealed class Square
        {
            public Vector3 Center;
            public List<Vector3Int> Cells;
            public readonly List<Square> Neighbours = new List<Square>();
        }

        /// <summary>
        /// Rebuilds the path from the walkable tilemaps: one position per connected group of cells, starting at the
        /// group nearest the start marker. Where the board splits, it keeps going straight (same direction as the
        /// previous step) and logs the fork, so a designer can check and edit the list afterwards.
        /// </summary>
        [ContextMenu("Auto-build From Tilemaps")]
        public void AutoBuild()
        {
            var squares = BuildSquares(ResolveTilemaps());
            if (squares.Count == 0)
            {
                Debug.LogError("[BoardPath] No walkable tiles found.", this);
                return;
            }

            var startPos = startMarker != null ? startMarker.position : FindStartPosition();
            var current = squares.OrderBy(s => (s.Center - startPos).sqrMagnitude).First();

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Auto-build Board Path");
#endif
            points.Clear();
            var visited = new HashSet<Square>();
            Vector3 direction = Vector3.zero;
            while (current != null)
            {
                points.Add(new PathPoint { position = current.Center, cells = current.Cells.OrderBy(c => c.y).ThenBy(c => c.x).ToList() });
                visited.Add(current);

                var options = current.Neighbours.Where(n => !visited.Contains(n)).ToList();
                if (options.Count == 0) break;

                var dir = direction;
                var next = dir == Vector3.zero
                    ? options.OrderByDescending(o => o.Center.x + o.Center.y).First() // from the start: go right/up
                    : options.OrderByDescending(o => Vector3.Dot((o.Center - current.Center).normalized, dir)).First();

                if (options.Count > 1)
                {
                    Debug.LogWarning($"[BoardPath] Fork at position {points.Count - 1}: continued straight. " +
                                     "Branches not on the path are ignored — edit the Points list if another route is intended.", this);
                }

                direction = (next.Center - current.Center).normalized;
                current = next;
            }

            Debug.Log($"[BoardPath] Built {points.Count} positions (0..{ExitIndex}).", this);
            Validate();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private Vector3 FindStartPosition()
        {
            GameObject tagged = null;
            try { tagged = GameObject.FindWithTag("Player"); } catch (UnityException) { }
            return tagged != null ? tagged.transform.position : transform.position;
        }

        private static List<Square> BuildSquares(Tilemap[] maps)
        {
            var squares = new List<Square>();
            var byCell = new Dictionary<Vector3Int, Square>();

            foreach (var map in maps)
            {
                map.CompressBounds();
                var cells = new HashSet<Vector3Int>();
                foreach (var cell in map.cellBounds.allPositionsWithin)
                {
                    if (map.HasTile(cell)) cells.Add(cell);
                }

                while (cells.Count > 0)
                {
                    var seed = cells.First();
                    var group = new List<Vector3Int>();
                    var open = new Stack<Vector3Int>();
                    open.Push(seed);
                    cells.Remove(seed);
                    while (open.Count > 0)
                    {
                        var c = open.Pop();
                        group.Add(c);
                        foreach (var n in Adjacent(c))
                        {
                            if (cells.Remove(n)) open.Push(n);
                        }
                    }

                    var center = Vector3.zero;
                    foreach (var c in group) center += map.GetCellCenterWorld(c);
                    var square = new Square { Center = center / group.Count, Cells = group };
                    squares.Add(square);
                    foreach (var c in group) byCell[c] = square;
                }
            }

            foreach (var square in squares)
            {
                foreach (var n in square.Cells.SelectMany(Adjacent))
                {
                    if (byCell.TryGetValue(n, out var other) && other != square && !square.Neighbours.Contains(other))
                    {
                        square.Neighbours.Add(other);
                    }
                }
            }

            return squares;
        }

        private static IEnumerable<Vector3Int> Adjacent(Vector3Int c)
        {
            yield return c + Vector3Int.right;
            yield return c + Vector3Int.left;
            yield return c + Vector3Int.up;
            yield return c + Vector3Int.down;
        }

        [ContextMenu("Validate")]
        public bool Validate()
        {
            if (points.Count < 2)
            {
                Debug.LogError("[BoardPath] The path needs a start and at least one position.", this);
                return false;
            }

            if (ExitIndex != expectedPlayablePositions)
            {
                Debug.LogWarning($"[BoardPath] Path has {ExitIndex} playable positions (1..{ExitIndex}); the design expects " +
                                 $"{expectedPlayablePositions}. The last position is treated as the exit.", this);
            }

            return true;
        }

        // ------------------------------------------------------------------ gizmos

        private void OnDrawGizmos()
        {
            if (points == null || points.Count == 0) return;

            for (var i = 0; i < points.Count; i++)
            {
                Gizmos.color = i == 0 ? Color.green : i == ExitIndex ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(points[i].position, 0.3f);
                if (i > 0)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(points[i - 1].position, points[i].position);
                }
#if UNITY_EDITOR
                UnityEditor.Handles.Label(points[i].position + Vector3.up * 0.6f, i == ExitIndex ? $"{i} EXIT" : i.ToString());
#endif
            }
        }
    }
}
