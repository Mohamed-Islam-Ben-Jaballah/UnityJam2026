using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DiceDrive.Board
{
    /// <summary>
    /// The level's movement graph: position 0 is the start. By default each position leads to the next one in the
    /// list and the last position is the exit. Where the board splits, a position lists several <c>next</c>
    /// positions (the player picks a route by clicking a destination on it), and a branch's last position is marked
    /// <c>isExit</c>. Each position is a world point (where the player stands) plus the tile cells it covers
    /// (for highlights and for matching enemies to positions).
    ///
    /// Edit the list in the Inspector, or use the context menu "Auto-build From Tilemaps" to generate it (forks
    /// included) from the walkable tilemaps. Positions and links are drawn in the Scene view.
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

            [Tooltip("Positions reachable from here. Empty = the next position in the list (or none if this is the " +
                     "last one / an exit). More than one = a fork.")]
            public List<int> next = new List<int>();

            [Tooltip("Reaching this position finishes the level (the last position always does).")]
            public bool isExit;
        }

        [Tooltip("Index 0 = start. Links default to the next index; see each point's 'next' for forks.")]
        [SerializeField] private List<PathPoint> points = new List<PathPoint>();

        [Tooltip("Designed number of moves from the start to an exit. Only used to warn when the path differs.")]
        [SerializeField, Min(1)] private int expectedPlayablePositions = 18;

        [Header("Auto-build")]
        [Tooltip("Walkable tilemaps. Each connected group of painted cells on one tilemap becomes one position.")]
        [SerializeField] private Tilemap[] walkableTilemaps;
        [SerializeField] private string[] walkableTilemapNames = { "Ground", "Parallel_Ground" };
        [Tooltip("Path starts on the position nearest to this (defaults to the object tagged Player).")]
        [SerializeField] private Transform startMarker;

        private static readonly int[] None = new int[0];

        public int Count => points.Count;
        public int ExpectedPlayablePositions => expectedPlayablePositions;
        public Tilemap ReferenceTilemap => ResolveTilemaps().FirstOrDefault();

        public Vector3 GetPosition(int index) => points[index].position;
        public IReadOnlyList<Vector3Int> GetCells(int index) => points[index].cells;

        /// <summary>Positions reachable in one step from <paramref name="index"/> (empty for an exit).</summary>
        public IReadOnlyList<int> Next(int index)
        {
            if (index < 0 || index >= points.Count) return None;
            var p = points[index];
            if (p.next != null && p.next.Count > 0) return p.next.Where(n => n > 0 && n < points.Count && n != index).ToArray();
            if (p.isExit || index == points.Count - 1) return None;
            return new[] { index + 1 };
        }

        public bool IsExit(int index) => Next(index).Count == 0;

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
        /// group nearest the start marker. The main route keeps going straight; every other branch at a fork is
        /// appended after it (the fork lists both as <c>next</c>) and each branch's end becomes an exit.
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
            var start = squares.OrderBy(s => (s.Center - startPos).sqrMagnitude).First();

#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Auto-build Board Path");
#endif
            points.Clear();
            var links = new List<List<int>>();
            var claimed = new HashSet<Square> { start };
            var branches = new Queue<(int from, Square square, Vector3 direction)>();
            branches.Enqueue((-1, start, Vector3.zero));
            var forks = 0;

            while (branches.Count > 0)
            {
                var (from, current, direction) = branches.Dequeue();
                while (current != null)
                {
                    var index = points.Count;
                    points.Add(new PathPoint { position = current.Center, cells = current.Cells.OrderBy(c => c.y).ThenBy(c => c.x).ToList() });
                    links.Add(new List<int>());
                    if (from >= 0) links[from].Add(index);

                    var options = current.Neighbours.Where(n => !claimed.Contains(n)).ToList();
                    if (options.Count == 0) break;

                    var dir = direction;
                    var ordered = dir == Vector3.zero
                        ? options.OrderByDescending(o => o.Center.x + o.Center.y).ToList() // from the start: go right/up
                        : options.OrderByDescending(o => Vector3.Dot((o.Center - current.Center).normalized, dir)).ToList();
                    foreach (var o in ordered) claimed.Add(o);

                    // Keep going straight; the other branches are appended once this route ends.
                    if (ordered.Count > 1) forks++;
                    foreach (var other in ordered.Skip(1))
                    {
                        branches.Enqueue((index, other, (other.Center - current.Center).normalized));
                    }

                    var next = ordered[0];
                    direction = (next.Center - current.Center).normalized;
                    from = index;
                    current = next;
                }
            }

            for (var i = 0; i < points.Count; i++)
            {
                var l = links[i];
                if (l.Count == 1 && l[0] == i + 1) continue; // default link
                if (l.Count == 0) points[i].isExit = i != points.Count - 1;
                else points[i].next = l;
            }

            Debug.Log($"[BoardPath] Built {points.Count} positions with {forks} fork(s).", this);
            Validate();
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
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
                    // A cell painted on several walkable tilemaps belongs to the first one, so a tile painted twice
                    // doesn't merge its neighbours on the other tilemap into one big position.
                    if (map.HasTile(cell) && !byCell.ContainsKey(cell)) cells.Add(cell);
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

            for (var i = 0; i < points.Count; i++)
            {
                if (points[i].next == null) continue;
                foreach (var n in points[i].next)
                {
                    if (n <= 0 || n >= points.Count || n == i)
                    {
                        Debug.LogWarning($"[BoardPath] Position {i} links to invalid position {n}; ignored.", this);
                    }
                }
            }

            // Shortest number of moves from the start to each reachable position.
            var depth = new Dictionary<int, int> { [0] = 0 };
            var open = new Queue<int>();
            open.Enqueue(0);
            while (open.Count > 0)
            {
                var i = open.Dequeue();
                foreach (var n in Next(i))
                {
                    if (depth.ContainsKey(n)) continue;
                    depth[n] = depth[i] + 1;
                    open.Enqueue(n);
                }
            }

            var exits = depth.Keys.Where(IsExit).OrderBy(i => i).ToList();
            if (exits.Count == 0)
            {
                Debug.LogError("[BoardPath] No exit is reachable from the start.", this);
                return false;
            }

            if (exits.All(e => depth[e] != expectedPlayablePositions))
            {
                Debug.LogWarning("[BoardPath] Moves from the start to each exit: " +
                                 string.Join(", ", exits.Select(e => $"exit {e} = {depth[e]}")) +
                                 $"; the design expects {expectedPlayablePositions}.", this);
            }

            return true;
        }

        // ------------------------------------------------------------------ gizmos

        private void OnDrawGizmos()
        {
            if (points == null || points.Count == 0) return;

            for (var i = 0; i < points.Count; i++)
            {
                var next = Next(i);
                var exit = next.Count == 0;
                Gizmos.color = i == 0 ? Color.green : exit ? Color.red : next.Count > 1 ? Color.cyan : Color.yellow;
                Gizmos.DrawWireSphere(points[i].position, 0.3f);

                Gizmos.color = Color.yellow;
                foreach (var n in next) Gizmos.DrawLine(points[i].position, points[n].position);
#if UNITY_EDITOR
                var label = exit ? $"{i} EXIT" : next.Count > 1 ? $"{i} FORK" : i.ToString();
                UnityEditor.Handles.Label(points[i].position + Vector3.up * 0.6f, label);
#endif
            }
        }
    }
}
