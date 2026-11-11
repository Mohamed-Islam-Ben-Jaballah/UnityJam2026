using System;
using UnityEngine;

namespace DiceDrive.Characters
{
    /// <summary>Animation states every character controller must contain (state names match the enum).</summary>
    public enum CharacterAnim
    {
        Idle,
        Run,
        Jump,
        Fall,
        Attack1,
        Attack2,
        Hit,
        Death
    }

    /// <summary>
    /// Presentation only: plays named animator states, flips the sprite to face a direction and forwards the
    /// attack-impact animation event. Knows nothing about gameplay. Lives on the sprite object next to the Animator.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class CharacterAnimator : MonoBehaviour
    {
        [Tooltip("True if the sprite art looks to the right when not flipped.")]
        [SerializeField] private bool artFacesRight = true;

        private Animator _animatorCache;
        private SpriteRenderer _rendererCache;

        // Lazy so callers can use this before its Awake runs (a parent's OnEnable runs before a child's Awake).
        private Animator _animator => _animatorCache != null ? _animatorCache : (_animatorCache = GetComponent<Animator>());
        private SpriteRenderer _renderer => _rendererCache != null ? _rendererCache : (_rendererCache = GetComponent<SpriteRenderer>());

        /// <summary>Raised by the "OnAttackImpact" animation event on the attack clips (the frame the blow lands).</summary>
        public event Action AttackImpact;

        /// <summary>Raised by the "OnFootstep" animation event on run clips (the frame a foot touches the ground).</summary>
        public event Action Footstep;

        public bool FacingRight => _renderer.flipX != artFacesRight;

        public CharacterAnim Current { get; private set; }

        public void Play(CharacterAnim anim)
        {
            Current = anim;
            _animator.Play(anim.ToString(), 0, 0f);
        }

        /// <summary>Faces left for negative x, right for positive x; ignores ~0 so vertical moves keep facing.</summary>
        public void Face(float directionX)
        {
            if (Mathf.Abs(directionX) < 0.001f)
            {
                return;
            }

            var wantRight = directionX > 0f;
            _renderer.flipX = wantRight != artFacesRight;
        }

        /// <summary>Length in seconds of the clip used by a state (0 if missing).</summary>
        public float GetLength(CharacterAnim anim)
        {
            var controller = _animator.runtimeAnimatorController;
            if (controller == null)
            {
                return 0f;
            }

            var name = anim.ToString();
            foreach (var clip in controller.animationClips)
            {
                if (clip.name == name)
                {
                    return clip.length;
                }
            }

            return 0f;
        }

        // Animation event receiver (keyed on the Attack1/Attack2 clips).
        private void OnAttackImpact()
        {
            AttackImpact?.Invoke();
        }

        // Animation event receiver (keyed on the Run clip's foot-contact frames).
        private void OnFootstep()
        {
            Footstep?.Invoke();
        }
    }
}
