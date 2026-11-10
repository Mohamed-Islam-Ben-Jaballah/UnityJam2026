using UnityEngine;

namespace DiceDrive.Characters
{
    /// <summary>
    /// Mirrors a character's animated sprite onto this renderer (which uses the "DiceDrive/Sprite Flame Aura"
    /// material) so a flame aura follows every animation frame and facing. Put it on a child of the character's
    /// sprite object with a lower sorting order, so the flames burn behind the body.
    /// <see cref="Intensity"/> can be raised from gameplay (e.g. flare up on attack, die down on death).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteFlameAura : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer source;

        [Tooltip("Overall strength, multiplied into the material's own intensity.")]
        [SerializeField, Range(0f, 2f)] private float intensity = 1f;

        [Tooltip("Slow breathing on top of the shader's flicker (0 = off).")]
        [SerializeField, Range(0f, 1f)] private float breathe = 0.2f;
        [SerializeField, Min(0f)] private float breatheSpeed = 1.3f;

        private SpriteRenderer _renderer;
        private float _seed;

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _seed = Random.value * 10f;
            if (source == null && transform.parent != null)
            {
                source = transform.parent.GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (source == null)
            {
                return;
            }

            // After the Animator has picked this frame's sprite.
            _renderer.sprite = source.sprite;
            _renderer.flipX = source.flipX;
            _renderer.flipY = source.flipY;

            var pulse = 1f - breathe * 0.5f * (1f + Mathf.Sin((Time.time + _seed) * breatheSpeed * Mathf.PI * 2f));
            var c = _renderer.color;
            c.a = Mathf.Clamp01(intensity * pulse);
            _renderer.color = c;
        }
    }
}
