using DiceDrive.Board;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace DiceDrive.EditorTools
{
    /// <summary>
    /// Builds the ambient VFX prefabs (devil ground, hallowed ground, candle flare) from the shared Ambient.vfx
    /// graph. Each prefab is a set of <see cref="AmbientVfxLayer"/> children. Tweak the prefabs directly, or
    /// change the specs here and re-run Tools > DiceDrive > Build Ambient VFX Prefabs.
    ///
    /// Look: vapor, not particles. Few, large, soft, slow, overlapping wisps at low opacity that blend into a
    /// volume; no individual dots or sparks.
    /// </summary>
    public static class AmbientVfxPrefabBuilder
    {
        private const string GraphPath = "Assets/_Game/Art/Board/VFX/Ambient.vfx";
        private const string VaporPath = "Assets/_Game/Art/Board/VFX/VFX_Vapor.png";
        private const string GlowPath = "Assets/_Game/Art/Board/VFX/VFX_Glow.png";
        private const string PrefabDir = "Assets/_Game/Prefabs/Board/";

        private enum Tex { Vapor, Glow }

        private sealed class Spec
        {
            public string Name;
            public Tex Texture = Tex.Vapor;
            public int Order = 16;
            public Vector2 Area = Vector2.one;
            public Vector2 Fraction = Vector2.one;
            public Vector2 Offset = Vector2.zero;
            public float Rate = 1f;
            public Vector2 Lifetime = new Vector2(1f, 2f);
            public Vector2 Size = new Vector2(0.1f, 0.2f);
            public Vector3 VelocityMin;
            public Vector3 VelocityMax;
            public float Rotation = 360f;
            public float Spin;
            public float Turbulence;
            public float TurbulenceFrequency = 1f;
            public Gradient Gradient;
            public Vector2 SizeOverLife = Vector2.one;
            public Color Tint = Color.white;
            public float Alpha = 1f;
        }

        [MenuItem("Tools/DiceDrive/Build Ambient VFX Prefabs")]
        public static void Build()
        {
            // ---- Devil ground: a crimson miasma pooled on the stone, hot wisps curling up from its base,
            //      and a dull infernal glow breathing underneath.
            Save("VFX_DevilGround",
                new Spec
                {
                    Name = "UnderGlow", Texture = Tex.Glow, Order = 16, Fraction = new Vector2(1f, 0.6f), Offset = new Vector2(0f, 0f),
                    Rate = 0.6f, Lifetime = new Vector2(2.5f, 4f), Size = new Vector2(1.6f, 2.4f), SizeOverLife = new Vector2(0.9f, 1.1f),
                    Alpha = 0.08f,
                    Gradient = Grad(new[] { new Color(0.85f, 0.12f, 0.03f), new Color(0.6f, 0.03f, 0.03f) }, Alpha(0f, 1f, 0.5f, 0f, 1f))
                },
                new Spec
                {
                    Name = "Miasma", Order = 16, Fraction = new Vector2(1f, 0.7f), Offset = new Vector2(0f, 0.1f),
                    Rate = 0.5f, Lifetime = new Vector2(5f, 8f), Size = new Vector2(1.3f, 2.2f),
                    VelocityMin = new Vector3(-0.06f, 0.02f), VelocityMax = new Vector3(0.06f, 0.05f), Spin = 4f,
                    Turbulence = 0.02f, TurbulenceFrequency = 0.3f, SizeOverLife = new Vector2(0.6f, 1.5f), Alpha = 0.16f,
                    Gradient = Grad(new[] { new Color(0.75f, 0.06f, 0.03f), new Color(0.35f, 0f, 0.04f) },
                        Alpha(new[] { 0f, 1f, 0.7f, 0f }, new[] { 0f, 0.3f, 0.65f, 1f }))
                },
                new Spec
                {
                    Name = "HeatWisps", Order = 17, Fraction = new Vector2(0.9f, 0.6f), Offset = new Vector2(0f, -0.05f),
                    Rate = 0.4f, Lifetime = new Vector2(3f, 5f), Size = new Vector2(0.5f, 0.9f),
                    VelocityMin = new Vector3(-0.05f, 0.18f), VelocityMax = new Vector3(0.05f, 0.25f), Spin = 8f,
                    Turbulence = 0.05f, TurbulenceFrequency = 0.7f, SizeOverLife = new Vector2(0.5f, 1.6f), Alpha = 0.09f,
                    Gradient = Grad(new[] { new Color(1f, 0.45f, 0.15f), new Color(0.8f, 0.1f, 0.04f), new Color(0.25f, 0f, 0.03f) },
                        new[] { 0f, 0.4f, 1f }, Alpha(0f, 1f, 0.2f, 0f, 1f))
                });

            // ---- Hallowed ground: low ghostly fog drifting sideways, pale spirit tendrils rising slowly.
            Save("VFX_HallowedGround",
                new Spec
                {
                    Name = "GhostFog", Order = 16, Fraction = new Vector2(1f, 0.7f), Offset = new Vector2(0f, 0.1f),
                    Rate = 0.4f, Lifetime = new Vector2(6f, 9f), Size = new Vector2(1.5f, 2.4f),
                    VelocityMin = new Vector3(0.03f, -0.01f), VelocityMax = new Vector3(0.1f, 0.03f), Spin = 3f,
                    Turbulence = 0.02f, TurbulenceFrequency = 0.3f, SizeOverLife = new Vector2(0.7f, 1.4f), Alpha = 0.11f,
                    Gradient = Grad(new[] { new Color(0.4f, 0.95f, 0.6f), new Color(0.6f, 0.9f, 0.85f) },
                        Alpha(new[] { 0f, 1f, 1f, 0f }, new[] { 0f, 0.35f, 0.65f, 1f }))
                },
                new Spec
                {
                    Name = "SpiritWisps", Order = 17, Fraction = new Vector2(0.9f, 0.6f), Offset = new Vector2(0f, -0.05f),
                    Rate = 0.25f, Lifetime = new Vector2(4f, 6f), Size = new Vector2(0.5f, 0.8f),
                    VelocityMin = new Vector3(-0.03f, 0.08f), VelocityMax = new Vector3(0.03f, 0.14f), Spin = 5f,
                    Turbulence = 0.04f, TurbulenceFrequency = 0.5f, SizeOverLife = new Vector2(0.6f, 1.5f), Alpha = 0.07f,
                    Gradient = Grad(new[] { new Color(0.75f, 1f, 0.85f), new Color(0.4f, 0.9f, 0.6f) }, Alpha(0f, 1f, 0.3f, 0f, 1f))
                });

            // ---- Candle flare: a smooth breathing glow (brightness driven by TorchFlicker) and a thin smoke trail.
            var flames = new Vector2(0.7f, 0.15f); // spans the three candle flames
            Save("VFX_CandleFlare",
                new Spec
                {
                    Name = "Halo", Texture = Tex.Glow, Order = 26, Area = flames, Rate = 57f,
                    Lifetime = new Vector2(0.8f, 1.3f), Size = new Vector2(1.1f, 1.5f), SizeOverLife = new Vector2(0.95f, 1.05f),
                    Alpha = 0.18f,
                    Gradient = Grad(new[] { new Color(1f, 0.75f, 0.4f), new Color(1f, 0.55f, 0.25f) }, Alpha(0f, 1f, 0.4f, 0f, 1f))
                },
                new Spec
                {
                    Name = "Smoke", Order = 26, Area = flames, Rate = 19f, Lifetime = new Vector2(2.5f, 3.5f), Size = new Vector2(0.15f, 0.25f),
                    VelocityMin = new Vector3(-0.03f, 0.25f), VelocityMax = new Vector3(0.03f, 0.4f), Spin = 30f,
                    Turbulence = 0.2f, TurbulenceFrequency = 1.2f, SizeOverLife = new Vector2(0.6f, 3f), Alpha = 0.07f,
                    Gradient = Grad(new[] { new Color(1f, 0.8f, 0.6f), new Color(0.6f, 0.55f, 0.6f) }, Alpha(0f, 1f, 0.15f, 0f, 1f))
                });

            AssetDatabase.SaveAssets();
            Debug.Log("Built ambient VFX prefabs.");
        }

        private static void Save(string prefabName, params Spec[] layers)
        {
            var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
            var vapor = AssetDatabase.LoadAssetAtPath<Texture2D>(VaporPath);
            var glow = AssetDatabase.LoadAssetAtPath<Texture2D>(GlowPath);

            var root = new GameObject(prefabName);
            foreach (var spec in layers)
            {
                var go = new GameObject(spec.Name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<VisualEffect>().visualEffectAsset = graph;

                var so = new SerializedObject(go.AddComponent<AmbientVfxLayer>());
                so.FindProperty("areaSize").vector2Value = spec.Area;
                so.FindProperty("spawnFraction").vector2Value = spec.Fraction;
                so.FindProperty("spawnOffset").vector2Value = spec.Offset;
                so.FindProperty("spawnRate").floatValue = spec.Rate;
                so.FindProperty("burstCount").floatValue = 0f;
                so.FindProperty("lifetime").vector2Value = spec.Lifetime;
                so.FindProperty("size").vector2Value = spec.Size;
                so.FindProperty("velocityMin").vector3Value = spec.VelocityMin;
                so.FindProperty("velocityMax").vector3Value = spec.VelocityMax;
                so.FindProperty("rotationRange").floatValue = spec.Rotation;
                so.FindProperty("spin").floatValue = spec.Spin;
                so.FindProperty("turbulenceIntensity").floatValue = spec.Turbulence;
                so.FindProperty("turbulenceFrequency").floatValue = spec.TurbulenceFrequency;
                so.FindProperty("texture").objectReferenceValue = spec.Texture == Tex.Glow ? glow : vapor;
                so.FindProperty("colorOverLife").gradientValue = spec.Gradient;
                so.FindProperty("sizeOverLife").animationCurveValue = AnimationCurve.EaseInOut(0f, spec.SizeOverLife.x, 1f, spec.SizeOverLife.y);
                so.FindProperty("tint").colorValue = spec.Tint;
                so.FindProperty("alpha").floatValue = spec.Alpha;
                so.FindProperty("sortingOrder").intValue = spec.Order;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        /// <summary>Fade in to full at <paramref name="peakTime"/>, then out.</summary>
        private static GradientAlphaKey[] Alpha(float start, float peak, float peakTime, float end, float endTime) =>
            new[] { new GradientAlphaKey(start, 0f), new GradientAlphaKey(peak, peakTime), new GradientAlphaKey(end, endTime) };

        private static GradientAlphaKey[] Alpha(float[] values, float[] times)
        {
            var keys = new GradientAlphaKey[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                keys[i] = new GradientAlphaKey(values[i], times[i]);
            }

            return keys;
        }

        private static Gradient Grad(Color[] colors, GradientAlphaKey[] alpha)
        {
            var times = new float[colors.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                times[i] = colors.Length == 1 ? 0f : (float)i / (colors.Length - 1);
            }

            return Grad(colors, times, alpha);
        }

        private static Gradient Grad(Color[] colors, float[] times, GradientAlphaKey[] alpha)
        {
            var keys = new GradientColorKey[colors.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                keys[i] = new GradientColorKey(colors[i], times[i]);
            }

            var g = new Gradient();
            g.SetKeys(keys, alpha);
            return g;
        }
    }
}
