using UnityEngine;
using UnityEngine.VFX;

namespace DiceDrive.Board
{
    /// <summary>
    /// One look (embers, mist, candle halo...) of the shared Ambient.vfx graph. Holds the values and pushes
    /// them into the VisualEffect's exposed properties. The emission area can be resized at runtime
    /// (e.g. to fit a board group) and the brightness driven by <see cref="Intensity"/> (e.g. candle flicker).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public sealed class AmbientVfxLayer : MonoBehaviour
    {
        [Header("Emission area")]
        [Tooltip("Size of the area this layer covers, in world units. Overridden by SetArea().")]
        [SerializeField] private Vector2 areaSize = Vector2.one;
        [Tooltip("Part of the area particles spawn in (1,1 = all of it).")]
        [SerializeField] private Vector2 spawnFraction = Vector2.one;
        [Tooltip("Offset of the spawn region, as a fraction of the area (0,-0.5 = bottom edge).")]
        [SerializeField] private Vector2 spawnOffset = Vector2.zero;

        [Header("Spawn")]
        [Tooltip("Particles per second per square unit of area.")]
        [SerializeField, Min(0f)] private float spawnRate = 2f;
        [SerializeField, Min(0f)] private float burstCount;
        [SerializeField, Min(0.05f)] private float burstPeriod = 2f;

        [Header("Particles")]
        [SerializeField] private Vector2 lifetime = new Vector2(1f, 2f);
        [SerializeField] private Vector2 size = new Vector2(0.1f, 0.2f);
        [SerializeField] private Vector3 velocityMin = new Vector3(-0.1f, 0.3f, 0f);
        [SerializeField] private Vector3 velocityMax = new Vector3(0.1f, 0.8f, 0f);
        [SerializeField, Range(0f, 360f)] private float rotationRange;
        [Tooltip("Max spin in degrees/second, random in [-spin, spin]. Makes vapor curl.")]
        [SerializeField, Min(0f)] private float spin;
        [SerializeField, Min(0f)] private float turbulenceIntensity = 0.5f;
        [SerializeField, Min(0f)] private float turbulenceFrequency = 1f;

        [Header("Look")]
        [SerializeField] private Texture2D texture;
        [SerializeField] private Gradient colorOverLife = new Gradient();
        [SerializeField] private AnimationCurve sizeOverLife = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color tint = Color.white;
        [SerializeField, Range(0f, 1f)] private float alpha = 1f;
        [SerializeField] private int sortingOrder = 16;

        private VisualEffect _vfx;
        private float _intensity = 1f;

        /// <summary>Brightness multiplier on top of the tint (1 = as authored).</summary>
        public float Intensity
        {
            get => _intensity;
            set
            {
                _intensity = value;
                if (Vfx != null)
                {
                    Vfx.SetVector3(Ids.Tint, TintVector());
                }
            }
        }

        private VisualEffect Vfx => _vfx != null ? _vfx : (_vfx = GetComponent<VisualEffect>());

        /// <summary>Resizes the emission area (world units) and scales the spawn rate with it.</summary>
        public void SetArea(Vector2 newSize)
        {
            areaSize = newSize;
            Apply();
        }

        /// <summary>Fast-forwards the effect so it starts populated instead of empty.</summary>
        public void Prewarm(float seconds)
        {
            const float step = 1f / 30f;
            Vfx.Simulate(step, (uint)Mathf.CeilToInt(seconds / step));
        }

        public void Apply()
        {
            var vfx = Vfx;
            if (vfx == null || vfx.visualEffectAsset == null)
            {
                return;
            }

            var region = Vector2.Scale(areaSize, spawnFraction);
            var center = Vector2.Scale(areaSize, spawnOffset);
            vfx.SetVector3(Ids.PositionMin, center - region * 0.5f);
            vfx.SetVector3(Ids.PositionMax, center + region * 0.5f);

            vfx.SetFloat(Ids.SpawnRate, spawnRate * Mathf.Max(0.01f, areaSize.x * areaSize.y));
            vfx.SetFloat(Ids.BurstCount, burstCount);
            vfx.SetFloat(Ids.BurstPeriod, burstPeriod);

            vfx.SetFloat(Ids.LifetimeMin, lifetime.x);
            vfx.SetFloat(Ids.LifetimeMax, lifetime.y);
            vfx.SetFloat(Ids.SizeMin, size.x);
            vfx.SetFloat(Ids.SizeMax, size.y);
            vfx.SetVector3(Ids.VelocityMin, velocityMin);
            vfx.SetVector3(Ids.VelocityMax, velocityMax);
            vfx.SetFloat(Ids.RotationRange, rotationRange);
            vfx.SetFloat(Ids.SpinMin, -spin);
            vfx.SetFloat(Ids.SpinMax, spin);
            vfx.SetFloat(Ids.TurbulenceIntensity, turbulenceIntensity);
            vfx.SetFloat(Ids.TurbulenceFrequency, turbulenceFrequency);

            if (texture != null)
            {
                vfx.SetTexture(Ids.MainTexture, texture);
            }

            vfx.SetGradient(Ids.ColorOverLife, colorOverLife);
            vfx.SetAnimationCurve(Ids.SizeOverLife, sizeOverLife);
            vfx.SetVector3(Ids.Tint, TintVector());
            vfx.SetFloat(Ids.Alpha, alpha);

            if (TryGetComponent<Renderer>(out var r))
            {
                r.sortingOrder = sortingOrder;
            }
        }

        private Vector3 TintVector() => new Vector3(tint.r, tint.g, tint.b) * _intensity;

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        private static class Ids
        {
            public static readonly int SpawnRate = Shader.PropertyToID("SpawnRate");
            public static readonly int BurstCount = Shader.PropertyToID("BurstCount");
            public static readonly int BurstPeriod = Shader.PropertyToID("BurstPeriod");
            public static readonly int PositionMin = Shader.PropertyToID("PositionMin");
            public static readonly int PositionMax = Shader.PropertyToID("PositionMax");
            public static readonly int LifetimeMin = Shader.PropertyToID("LifetimeMin");
            public static readonly int LifetimeMax = Shader.PropertyToID("LifetimeMax");
            public static readonly int SizeMin = Shader.PropertyToID("SizeMin");
            public static readonly int SizeMax = Shader.PropertyToID("SizeMax");
            public static readonly int VelocityMin = Shader.PropertyToID("VelocityMin");
            public static readonly int VelocityMax = Shader.PropertyToID("VelocityMax");
            public static readonly int RotationRange = Shader.PropertyToID("RotationRange");
            public static readonly int SpinMin = Shader.PropertyToID("SpinMin");
            public static readonly int SpinMax = Shader.PropertyToID("SpinMax");
            public static readonly int TurbulenceIntensity = Shader.PropertyToID("TurbulenceIntensity");
            public static readonly int TurbulenceFrequency = Shader.PropertyToID("TurbulenceFrequency");
            public static readonly int MainTexture = Shader.PropertyToID("MainTexture");
            public static readonly int ColorOverLife = Shader.PropertyToID("ColorOverLife");
            public static readonly int SizeOverLife = Shader.PropertyToID("SizeOverLife");
            public static readonly int Tint = Shader.PropertyToID("Tint");
            public static readonly int Alpha = Shader.PropertyToID("Alpha");
        }
    }
}
