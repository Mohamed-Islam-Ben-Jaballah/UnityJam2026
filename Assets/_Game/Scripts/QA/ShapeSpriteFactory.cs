using System;
using UnityEngine;

namespace DiceDrive.QA
{
    public static class ShapeSpriteFactory
    {
        private const int Size = 64;
        private static Sprite _square;
        private static Sprite _circle;
        private static Sprite _triangle;
        private static Sprite _hexagon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            _square = null;
            _circle = null;
            _triangle = null;
            _hexagon = null;
        }

        public static Sprite Square => Get(ref _square, "Square", (x, y) => true);
        public static Sprite Circle => Get(ref _circle, "Circle", InCircle);
        public static Sprite Triangle => Get(ref _triangle, "Triangle", InTriangle);
        public static Sprite Hexagon => Get(ref _hexagon, "Hexagon", InHexagon);

        private static Sprite Get(ref Sprite cache, string name, Func<float, float, bool> inside)
        {
            if (cache == null)
            {
                cache = Create(name, inside);
            }

            return cache;
        }

        private static Sprite Create(string name, Func<float, float, bool> inside)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var u = (x + 0.5f) / Size;
                    var v = (y + 0.5f) / Size;
                    pixels[y * Size + x] = inside(u, v)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        private static bool InCircle(float x, float y)
        {
            var dx = x - 0.5f;
            var dy = y - 0.5f;
            return dx * dx + dy * dy <= 0.45f * 0.45f;
        }

        private static bool InTriangle(float x, float y)
        {
            var a = new Vector2(0.5f, 0.92f);
            var b = new Vector2(0.08f, 0.08f);
            var c = new Vector2(0.92f, 0.08f);
            var p = new Vector2(x, y);

            var d1 = Sign(p, a, b);
            var d2 = Sign(p, b, c);
            var d3 = Sign(p, c, a);
            var hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            var hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static bool InHexagon(float x, float y)
        {
            var p = new Vector2(x, y);
            var inside = false;
            var vertices = new Vector2[6];

            for (var i = 0; i < 6; i++)
            {
                var angle = Mathf.Deg2Rad * (60f * i + 30f);
                vertices[i] = new Vector2(0.5f + 0.46f * Mathf.Cos(angle), 0.5f + 0.46f * Mathf.Sin(angle));
            }

            for (var i = 0; i < 6; i++)
            {
                var j = (i + 5) % 6;
                if ((vertices[i].y > p.y) != (vertices[j].y > p.y) &&
                    p.x < (vertices[j].x - vertices[i].x) * (p.y - vertices[i].y) /
                    (vertices[j].y - vertices[i].y) + vertices[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}
