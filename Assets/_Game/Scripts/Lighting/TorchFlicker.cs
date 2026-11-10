using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DiceDrive.Lighting
{
    /// <summary>
    /// Candle/torch flicker: noise-driven Light2D intensity and radius, plus an optional looping
    /// sprite flame animation (the RF Castle clips don't loop, so frames are cycled here).
    /// </summary>
    public sealed class TorchFlicker : MonoBehaviour
    {
        [SerializeField] private Light2D light2D;

        [Header("Flicker")]
        [SerializeField, Min(0f)] private float baseIntensity = 1.2f;
        [SerializeField, Range(0f, 1f)] private float intensityJitter = 0.25f;
        [SerializeField, Range(0f, 1f)] private float radiusJitter = 0.08f;
        [SerializeField, Min(0f)] private float speed = 6f;

        [Tooltip("Optional glow VFX whose brightness follows the light's flicker.")]
        [SerializeField] private DiceDrive.Board.AmbientVfxLayer halo;

        [Header("Flame sprite (optional)")]
        [SerializeField] private SpriteRenderer flameRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;

        private float _seed;
        private float _baseRadius;

        private void Awake()
        {
            _seed = Random.value * 100f;
            if (light2D == null)
            {
                light2D = GetComponentInChildren<Light2D>();
            }

            if (light2D != null)
            {
                _baseRadius = light2D.pointLightOuterRadius;
            }
        }

        private void Update()
        {
            var t = Time.time * speed + _seed;

            if (light2D != null)
            {
                // Two noise octaves: slow breathing + fast crackle.
                var n = Mathf.PerlinNoise(t * 0.35f, _seed) * 0.7f + Mathf.PerlinNoise(t, _seed + 7f) * 0.3f;
                var k = (n - 0.5f) * 2f;
                light2D.intensity = baseIntensity * (1f + k * intensityJitter);
                light2D.pointLightOuterRadius = _baseRadius * (1f + k * radiusJitter);
            }

            if (halo != null)
            {
                // Same noise as the light, slightly exaggerated so the glow visibly breathes.
                var n = Mathf.PerlinNoise(t * 0.35f, _seed) * 0.7f + Mathf.PerlinNoise(t, _seed + 7f) * 0.3f;
                halo.Intensity = 1f + (n - 0.5f) * 2f * intensityJitter * 1.5f;
            }

            if (flameRenderer != null && frames != null && frames.Length > 0)
            {
                var index = (int)((Time.time + _seed) * framesPerSecond) % frames.Length;
                flameRenderer.sprite = frames[index];
            }
        }
    }
}
