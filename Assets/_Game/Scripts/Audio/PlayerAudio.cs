using DiceDrive.Characters;
using UnityEngine;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays the player's sounds by listening to the character scripts; they have no audio code themselves.
    /// - Footsteps: each time the run animation puts a foot down (OnFootstep animation events on the Run clip).
    /// - Attack: the moment the player's blow lands (<see cref="CharacterActor.AttackLanded"/>).
    /// Lives on the player's sprite object, next to the CharacterAnimator (the CharacterActor is on the parent).
    /// </summary>
    [RequireComponent(typeof(CharacterAnimator))]
    public sealed class PlayerAudio : MonoBehaviour
    {
        private CharacterAnimator animator;
        private CharacterActor actor;

        private void Awake()
        {
            animator = GetComponent<CharacterAnimator>();
            actor = GetComponentInParent<CharacterActor>();
        }

        private void OnEnable()
        {
            animator.Footstep += OnFootstep;
            if (actor != null) actor.AttackLanded += OnAttackLanded;
        }

        private void OnDisable()
        {
            animator.Footstep -= OnFootstep;
            if (actor != null) actor.AttackLanded -= OnAttackLanded;
        }

        private void Start()
        {
            // Load the sounds up front so the first step or swing doesn't stall while FMOD reads them from disk.
            if (!AudioReady) return;
            AudioManager.instance.LoadSampleData(FMODEvents.instance.PlayerWalk);
            AudioManager.instance.LoadSampleData(FMODEvents.instance.PlayerAttack);
        }

        private void OnFootstep()
        {
            if (AudioReady) AudioManager.instance.PlayOneShot(FMODEvents.instance.PlayerWalk);
        }

        private void OnAttackLanded(CharacterActor attacker)
        {
            if (AudioReady) AudioManager.instance.PlayOneShot(FMODEvents.instance.PlayerAttack);
        }

        // False in scenes without the audio objects, which then stay silent.
        private static bool AudioReady => AudioManager.instance != null && FMODEvents.instance != null;
    }
}
