using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DiceDrive.Board
{
    /// <summary>
    /// Mirrors every cell painted on the source (Ground) tilemap onto this tilemap with a glow tile,
    /// tinted in a chessboard pattern. In play mode the glow pulses as a wave along X, and gameplay
    /// code can highlight cells (e.g. the squares reachable with the current dice roll).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Tilemap))]
    public sealed class BoardTileGlow : MonoBehaviour
    {
        [Tooltip("Tilemap whose painted cells are the walkable board squares.")]
        [SerializeField] private Tilemap source;

        [SerializeField] private TileBase glowTile;

        [Tooltip("Optional: 16 tiles indexed by which sides get a border (1 = up, 2 = right, 4 = down, 8 = left). " +
                 "A side is bordered when the neighbouring cell is not a board cell, so each connected group of " +
                 "cells gets one outline. Leave empty to use Glow Tile everywhere.")]
        [SerializeField] private TileBase[] edgeTiles = new TileBase[0];

        [Header("Chessboard")]
        [Tooltip("Square colors. Each row is offset by two entries, so with 4 colors no square touches one of the same color.")]
        [SerializeField] private Color[] palette =
        {
            new Color32(0x15, 0x5b, 0x54, 0xff),
            new Color32(0x31, 0x15, 0x5b, 0xff),
            new Color32(0x5b, 0x15, 0x1c, 0xff),
            new Color32(0x3f, 0x5b, 0x15, 0xff)
        };

        [Tooltip("How many tiles wide/tall one board square is.")]
        [SerializeField, Min(1)] private int squareSize = 1;

        [Header("Pulse (play mode only)")]
        [SerializeField, Min(0f)] private float pulseSpeed = 2f;
        [SerializeField, Range(0f, 1f)] private float pulseAmount = 0.4f;

        [Tooltip("Cells per ripple travelling along the path. 0 = all cells pulse together.")]
        [SerializeField, Min(0f)] private float waveLength = 6f;

        private Tilemap _glow;
        private readonly List<Vector3Int> _cells = new List<Vector3Int>();
        private readonly HashSet<Vector3Int> _cellSet = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, Color> _highlights = new Dictionary<Vector3Int, Color>();
        private bool _dirty;

        /// <summary>All board cells, in tilemap cell coordinates.</summary>
        public IReadOnlyList<Vector3Int> Cells => _cells;

        public bool IsBoardCell(Vector3Int cell) => _cellSet.Contains(cell);

        /// <summary>Overrides the glow color of the given cells until cleared.</summary>
        public void SetHighlights(IEnumerable<Vector3Int> cells, Color color)
        {
            _highlights.Clear();
            foreach (var cell in cells)
            {
                _highlights[cell] = color;
            }

            ApplyColors(Application.isPlaying ? Time.time : 0f);
        }

        public void ClearHighlights()
        {
            _highlights.Clear();
            ApplyColors(Application.isPlaying ? Time.time : 0f);
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (_glow == null)
            {
                _glow = GetComponent<Tilemap>();
            }

            _cells.Clear();
            _cellSet.Clear();

            if (source != null && (glowTile != null || (edgeTiles != null && edgeTiles.Length == 16)))
            {
                foreach (var cell in source.cellBounds.allPositionsWithin)
                {
                    if (source.HasTile(cell))
                    {
                        _cells.Add(cell);
                        _cellSet.Add(cell);
                    }
                }
            }

            // Remove glow from cells that are no longer board cells.
            var stale = new List<Vector3Int>();
            foreach (var cell in _glow.cellBounds.allPositionsWithin)
            {
                if (_glow.HasTile(cell) && !_cellSet.Contains(cell))
                {
                    stale.Add(cell);
                }
            }

            foreach (var cell in stale)
            {
                _glow.SetTile(cell, null);
            }

            // Add glow to new board cells (only touches cells that changed, so the scene stays clean).
            foreach (var cell in _cells)
            {
                var tile = TileFor(cell);
                if (_glow.GetTile(cell) != tile)
                {
                    _glow.SetTile(cell, tile);
                }

                _glow.SetTileFlags(cell, TileFlags.None);
            }

            ApplyColors(Application.isPlaying ? Time.time : 0f);
        }

        private void OnEnable()
        {
            _glow = GetComponent<Tilemap>();
            Tilemap.tilemapTileChanged += OnTilemapChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            Tilemap.tilemapTileChanged -= OnTilemapChanged;
        }

        private void OnValidate()
        {
            // Tilemaps can't be modified from OnValidate; defer to the next Update.
            _dirty = true;
        }

        private void OnTilemapChanged(Tilemap tilemap, Tilemap.SyncTile[] tiles)
        {
            if (tilemap == source)
            {
                _dirty = true;
            }
        }

        private void Update()
        {
            if (_dirty)
            {
                _dirty = false;
                Rebuild();
            }

            if (Application.isPlaying)
            {
                ApplyColors(Time.time);
            }
        }

        private void ApplyColors(float time)
        {
            if (_glow == null)
            {
                return;
            }

            var animate = Application.isPlaying && pulseAmount > 0f;
            var waveScale = waveLength > 0f ? (2f * Mathf.PI) / waveLength : 0f;

            foreach (var cell in _cells)
            {
                var color = _highlights.TryGetValue(cell, out var highlight) ? highlight : BaseColor(cell);

                if (animate)
                {
                    var wave = 0.5f + 0.5f * Mathf.Sin(time * pulseSpeed - cell.x * waveScale);
                    color.a *= Mathf.Lerp(1f - pulseAmount, 1f, wave);
                }

                if (_glow.GetColor(cell) != color)
                {
                    _glow.SetColor(cell, color);
                }
            }
        }

        private TileBase TileFor(Vector3Int cell)
        {
            if (edgeTiles == null || edgeTiles.Length != 16)
            {
                return glowTile;
            }

            var mask = 0;
            if (!_cellSet.Contains(cell + Vector3Int.up)) mask |= 1;
            if (!_cellSet.Contains(cell + Vector3Int.right)) mask |= 2;
            if (!_cellSet.Contains(cell + Vector3Int.down)) mask |= 4;
            if (!_cellSet.Contains(cell + Vector3Int.left)) mask |= 8;
            return edgeTiles[mask] != null ? edgeTiles[mask] : glowTile;
        }

        private Color BaseColor(Vector3Int cell)
        {
            var sx = Mathf.FloorToInt((float)cell.x / squareSize);
            var sy = Mathf.FloorToInt((float)cell.y / squareSize);
            if (palette == null || palette.Length == 0)
            {
                return Color.white;
            }

            var n = palette.Length;
            var offset = n >= 4 ? 2 : 1; // 4+ colors: diagonal shift; 2-3 colors: classic chessboard
            var index = ((sx + sy * offset) % n + n) % n;
            return palette[index];
        }
    }
}
