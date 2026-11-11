using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DiceDrive.Board
{
    /// <summary>
    /// Smoothly keeps an orthographic camera on the player, clamped so it never shows past the level's tiles.
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

        private Camera _camera;
        private Bounds _bounds;
        private bool _hasBounds;
        private Vector3 _velocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

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

        private Vector3 Desired()
        {
            var p = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            if (!clampToBounds || !_hasBounds || !_camera.orthographic) return p;

            var halfH = _camera.orthographicSize;
            var halfW = halfH * _camera.aspect;
            p.x = ClampAxis(p.x, _bounds.min.x + halfW, _bounds.max.x - halfW, _bounds.center.x);
            p.y = ClampAxis(p.y, _bounds.min.y + halfH, _bounds.max.y - halfH, _bounds.center.y);
            return p;
        }

        // When the level is smaller than the view on an axis, centre on it instead.
        private static float ClampAxis(float v, float min, float max, float center) =>
            min > max ? center : Mathf.Clamp(v, min, max);
    }
}
