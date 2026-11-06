using DiceDrive.Cards;
using DiceDrive.Grid;
using DiceDrive.Turn;
using UnityEngine;

namespace DiceDrive.QA
{
    public sealed class QaTargetingController : MonoBehaviour
    {
        private static readonly Color SafeColor = new Color(0.2f, 1f, 0.3f, 1f);
        private static readonly Color DamageColor = new Color(1f, 0.55f, 0.1f, 1f);
        private static readonly Color BlockedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

        private GameSession _session;
        private Camera _camera;
        private float _tileSize;
        private LineRenderer _dragLine;
        private LineRenderer _pathLine;
        private SpriteRenderer _highlight;
        private CardInstance _card;
        private Vector2 _dragStartScreen;
        private bool _dragging;

        public void Initialize(Camera camera, float tileSize)
        {
            _camera = camera;
            _tileSize = tileSize;
            _dragLine = CreateLine("Drag Line", 6);
            _pathLine = CreateLine("Path Line", 7);
            _highlight = CreateHighlight();
            HideVisuals();
        }

        public void SetSession(GameSession session)
        {
            _session = session;
            CancelDrag();
        }

        public void BeginDrag(CardInstance card, Vector2 startScreen)
        {
            _card = card;
            _dragStartScreen = startScreen;
            _dragging = true;
        }

        public void UpdateHover(Vector2 screenPosition)
        {
            if (!_dragging || _session == null || _card == null || _card.Data == null)
            {
                HideVisuals();
                return;
            }

            if (!TryGetCoord(screenPosition, out var coord))
            {
                HideVisuals();
                return;
            }

            var canPlay = false;
            var state = PathState.Safe;
            PathResult path = null;

            if (IsSelfCard(_card))
            {
                canPlay = _session.CanPlay(_card, _session.Player.Position, out _);
            }
            else if (_card.Data.effect == CardEffectType.Walk)
            {
                var paths = _session.GetWalkPaths();
                if (paths.TryGetValue(coord, out path))
                {
                    canPlay = true;
                    state = path.State;
                }
            }
            else
            {
                canPlay = _session.CanPlay(_card, coord, out _);
            }

            var color = !canPlay
                ? BlockedColor
                : state == PathState.Damaging ? DamageColor : SafeColor;

            DrawLine(_dragLine, ScreenToWorld(_dragStartScreen), ScreenToWorld(screenPosition), color);
            SetHighlight(coord, color);

            if (path != null && path.Tiles.Count > 1)
            {
                DrawPath(path, color);
            }
            else
            {
                _pathLine.positionCount = 0;
            }
        }

        public void EndDrag(Vector2 screenPosition)
        {
            if (!_dragging || _session == null || _card == null)
            {
                CancelDrag();
                return;
            }

            var target = _session.Player.Position;
            var valid = IsSelfCard(_card) || TryGetCoord(screenPosition, out target);

            if (valid && _session.CanPlay(_card, target, out _))
            {
                _session.PlayCard(_card, target);
            }

            CancelDrag();
        }

        public void CancelDrag()
        {
            _dragging = false;
            _card = null;
            _dragStartScreen = default;
            HideVisuals();
        }

        private void HideVisuals()
        {
            if (_dragLine != null)
            {
                _dragLine.positionCount = 0;
            }

            if (_pathLine != null)
            {
                _pathLine.positionCount = 0;
            }

            if (_highlight != null)
            {
                _highlight.enabled = false;
            }
        }

        private LineRenderer CreateLine(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = 0.06f;
            line.endWidth = 0.06f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = sortingOrder;
            line.positionCount = 0;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader != null)
            {
                line.material = new Material(shader);
            }

            return line;
        }

        private SpriteRenderer CreateHighlight()
        {
            var go = new GameObject("Tile Highlight");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = ShapeSpriteFactory.Square;
            renderer.sortingOrder = 2;
            renderer.enabled = false;
            return renderer;
        }

        private void DrawLine(LineRenderer line, Vector3 from, Vector3 to, Color color)
        {
            if (line == null)
            {
                return;
            }

            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startColor = color;
            line.endColor = color;
        }

        private void DrawPath(PathResult path, Color color)
        {
            if (_pathLine == null)
            {
                return;
            }

            _pathLine.positionCount = path.Tiles.Count;
            _pathLine.startColor = color;
            _pathLine.endColor = color;
            _pathLine.startWidth = 0.12f;
            _pathLine.endWidth = 0.12f;

            for (var i = 0; i < path.Tiles.Count; i++)
            {
                _pathLine.SetPosition(i, CoordToWorld(path.Tiles[i]));
            }
        }

        private void SetHighlight(GridCoord coord, Color color)
        {
            if (_highlight == null)
            {
                return;
            }

            _highlight.enabled = true;
            _highlight.transform.position = CoordToWorld(coord) + new Vector3(0f, 0f, 0.1f);
            _highlight.transform.localScale = Vector3.one * (_tileSize * 0.85f);
            _highlight.color = new Color(color.r, color.g, color.b, 0.35f);
        }

        private bool TryGetCoord(Vector2 screenPosition, out GridCoord coord)
        {
            coord = default;
            if (_camera == null)
            {
                return false;
            }

            var world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            var x = Mathf.RoundToInt(world.x / _tileSize);
            var y = Mathf.RoundToInt(world.y / _tileSize);
            coord = new GridCoord(x, y);
            return _session != null && _session.Grid.InBounds(coord);
        }

        private Vector3 CoordToWorld(GridCoord coord)
        {
            return new Vector3(coord.X * _tileSize, coord.Y * _tileSize, -0.5f);
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            var world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            world.z = -0.6f;
            return world;
        }

        private static bool IsSelfCard(CardInstance card)
        {
            return card != null && card.Data != null && card.Data.rangeType == CardRangeType.Self;
        }
    }
}
