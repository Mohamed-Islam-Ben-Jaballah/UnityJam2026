using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DiceDrive.Board
{
    /// <summary>
    /// Smoothly keeps an orthographic camera on the player, clamped so it never shows past the level's tiles.
    /// When a <see cref="DiceBoardMovement"/> is present it also frames the next <c>lookAheadTiles</c> path
    /// positions (by panning only; the zoom never changes) so every reachable destination tile is on screen and clickable.
    /// Put it on the Main Camera. Target defaults to the object tagged "Player"; bounds default to every
    /// tilemap in the scene.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";

        [Tooltip("Camera position relative to the target (z is ignored; the camera keeps its own z).")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

        [Tooltip("Seconds to catch up with the target. 0 = locked on.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.25f;

        [Tooltip("Tilemaps the view must stay inside. Empty = all tilemaps in the scene.")]
        [SerializeField] private Tilemap[] bounds;
        [SerializeField] private bool clampToBounds = true;
        [Tooltip("Extra world units allowed above the highest tiles, so characters standing on the top lane " +
                 "(and the sky behind them) stay on screen.")]
        [SerializeField, Min(0f)] private float topHeadroom = 4f;

        [Header("Look-ahead")]
        [Tooltip("Board whose upcoming path tiles stay in view. Auto-found when empty.")]
        [SerializeField] private DiceBoardMovement board;
        [Tooltip("How many path positions ahead of the player must always be visible.")]
        [SerializeField, Min(0)] private int lookAheadTiles = 6;
        [Tooltip("World-unit margin kept around the framed tiles.")]
        [SerializeField, Min(0f)] private float framePadding = 1f;

        private Camera _camera;
        private Bounds _bounds;
        private bool _hasBounds;
        private Vector3 _velocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (board == null) board = FindAnyObjectByType<DiceBoardMovement>();

            if (target == null)
            {
                GameObject tagged = null;
                try { tagged = GameObject.FindWithTag(targetTag); } catch (UnityException) { /* tag not defined */ }
                if (tagged != null) target = tagged.transform;
            }

            var maps = bounds != null && bounds.Any(b => b != null)
                ? bounds.Where(b => b != null).ToArray()
                : FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

            foreach (var map in maps)
            {
                map.CompressBounds();
                if (map.cellBounds.size.x == 0 || map.cellBounds.size.y == 0) continue;

                var min = map.CellToWorld(map.cellBounds.min);
                var max = map.CellToWorld(map.cellBounds.max);
                var b = new Bounds((min + max) / 2f, max - min);
                if (_hasBounds) _bounds.Encapsulate(b);
                else { _bounds = b; _hasBounds = true; }
            }

            if (_hasBounds) _bounds.Encapsulate(new Vector3(_bounds.center.x, _bounds.max.y + topHeadroom, 0f));
        }

        private void Start()
        {
            if (target != null) transform.position = Desired(); // start framed on the player
        }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = smoothTime <= 0f
                ? Desired()
                : Vector3.SmoothDamp(transform.position, Desired(), ref _velocity, smoothTime);
        }

        /// <summary>World rect covering the player and the next look-ahead path positions.</summary>
        private bool TryGetFrame(out Rect frame)
        {
            frame = default;
            var path = board != null ? board.Path : null;
            if (path == null || lookAheadTiles <= 0 || board.Session == null) return false;

            // Every position within lookAheadTiles moves, down every branch of a fork.
            Vector2 min = target.position, max = target.position;
            var layer = new List<int> { board.Position };
            for (var step = 0; step <= lookAheadTiles && layer.Count > 0; step++)
            {
                var nextLayer = new List<int>();
                foreach (var i in layer)
                {
                    var p = path.GetPosition(i);
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                    if (step < lookAheadTiles) nextLayer.AddRange(path.Next(i));
                }

                layer = nextLayer;
            }

            frame = Rect.MinMaxRect(min.x - framePadding, min.y - framePadding, max.x + framePadding, max.y + framePadding);
            return true;
        }

        private Vector3 Desired()
        {
            var p = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

            if (_camera.orthographic && TryGetFrame(out var frame))
            {
                // Start from the player, then shift just enough that the whole look-ahead frame is on screen.
                var hh = _camera.orthographicSize;
                var hw = hh * _camera.aspect;
                p.x = FitAxis(p.x, frame.xMin, frame.xMax, hw);
                p.y = FitAxis(p.y, frame.yMin, frame.yMax, hh);
            }

            if (!clampToBounds || !_hasBounds || !_camera.orthographic) return p;

            var halfH = _camera.orthographicSize;
            var halfW = halfH * _camera.aspect;
            p.x = ClampAxis(p.x, _bounds.min.x + halfW, _bounds.max.x - halfW, _bounds.center.x);
            p.y = ClampAxis(p.y, _bounds.min.y + halfH, _bounds.max.y - halfH, _bounds.center.y);
            return p;
        }

        // Moves the camera centre v the least so [min, max] fits in [v - half, v + half]; centres it if it can't fit.
        private static float FitAxis(float v, float min, float max, float half)
        {
            if (max - min >= 2f * half) return (min + max) / 2f;
            return Mathf.Clamp(v, max - half, min + half);
        }

        // When the level is smaller than the view on an axis, centre on it instead.
        private static float ClampAxis(float v, float min, float max, float center) =>
            min > max ? center : Mathf.Clamp(v, min, max);
    }
}
