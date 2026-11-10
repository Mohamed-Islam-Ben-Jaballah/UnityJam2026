using System.Collections.Generic;
using UnityEngine;

namespace DiceDrive.Board
{
    /// <summary>
    /// Spawns one ambient VFX prefab per connected group of board cells (e.g. each 3x2 block of a glow layer).
    /// Every <see cref="AmbientVfxLayer"/> in the prefab gets its emission area set to the group's size
    /// (spawn rate scales with the area).
    /// </summary>
    public sealed class BoardGroupVfx : MonoBehaviour
    {
        [SerializeField] private BoardTileGlow board;
        [SerializeField] private GameObject vfxPrefab;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void Start()
        {
            Spawn();
        }

        [ContextMenu("Respawn")]
        public void Spawn()
        {
            Clear();
            if (board == null || vfxPrefab == null)
            {
                return;
            }

            var tilemap = board.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            foreach (var group in FindGroups())
            {
                var min = group[0];
                var max = group[0];
                foreach (var cell in group)
                {
                    min = Vector3Int.Min(min, cell);
                    max = Vector3Int.Max(max, cell);
                }

                var center = (tilemap.GetCellCenterWorld(min) + tilemap.GetCellCenterWorld(max)) * 0.5f;
                var size = Vector3.Scale(max - min + Vector3Int.one, tilemap.layoutGrid.cellSize);

                var vfx = Instantiate(vfxPrefab, center, Quaternion.identity, transform);
                vfx.name = $"{vfxPrefab.name} {min.x},{min.y}";
                Fit(vfx, size);
                _spawned.Add(vfx);
            }
        }

        private void Clear()
        {
            foreach (var go in _spawned)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _spawned.Clear();
        }

        private static void Fit(GameObject vfx, Vector3 size)
        {
            foreach (var layer in vfx.GetComponentsInChildren<AmbientVfxLayer>(true))
            {
                layer.SetArea(size);

                // Start populated, and at a different point per group so they don't flicker in sync.
                layer.Prewarm(Random.Range(2f, 5f));
            }
        }

        private List<List<Vector3Int>> FindGroups()
        {
            var groups = new List<List<Vector3Int>>();
            var visited = new HashSet<Vector3Int>();
            var stack = new Stack<Vector3Int>();
            var dirs = new[] { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

            foreach (var start in board.Cells)
            {
                if (!visited.Add(start))
                {
                    continue;
                }

                var group = new List<Vector3Int>();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var cell = stack.Pop();
                    group.Add(cell);
                    foreach (var d in dirs)
                    {
                        var next = cell + d;
                        if (board.IsBoardCell(next) && visited.Add(next))
                        {
                            stack.Push(next);
                        }
                    }
                }

                groups.Add(group);
            }

            return groups;
        }
    }
}
