using DiceDrive.Characters;
using FMODUnity;
using UnityEngine;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays an enemy's sounds by listening to its <see cref="CharacterActor"/>; the actor has no audio code.
    /// One script for every enemy type: each enemy prefab picks its own sounds in the Inspector.
    /// Currently: the attack sound, the moment the enemy's blow lands.
    /// Lives on the enemy's root object, next to the CharacterActor.
    /// </summary>
    [RequireComponent(typeof(CharacterActor))]
    public sealed class EnemyAudio : MonoBehaviour
    {
        /// <summary>Which attack sound from <see cref="FMODEvents"/> this enemy uses.</summary>
        public enum AttackSound
        {
            None,
            Skeleton,
            Dagger,
            WoodenCreature
        }

        [Tooltip("Attack sound played when this enemy's blow lands.")]
        [SerializeField] private AttackSound attackSound;

        private CharacterActor actor;

        private void Awake() => actor = GetComponent<CharacterActor>();

        private void OnEnable() => actor.AttackLanded += OnAttackLanded;

        private void OnDisable() => actor.AttackLanded -= OnAttackLanded;

        private void Start()
        {
            // Load the sound up front so the first attack doesn't stall while FMOD reads it from disk.
            if (AudioReady && attackSound != AttackSound.None)
                AudioManager.instance.LoadSampleData(AttackEvent());
        }

        private void OnAttackLanded(CharacterActor attacker)
        {
            if (AudioReady && attackSound != AttackSound.None)
                AudioManager.instance.PlayOneShot(AttackEvent());
        }

        private EventReference AttackEvent()
        {
            var events = FMODEvents.instance;
            switch (attackSound)
            {
                case AttackSound.Skeleton: return events.SkeletonAttack;
                case AttackSound.Dagger: return events.DaggerAttack;
                case AttackSound.WoodenCreature: return events.WoodenCreatureAttack;
                default: return default;
            }
        }

        // False in scenes without the audio objects, which then stay silent.
        private static bool AudioReady => AudioManager.instance != null && FMODEvents.instance != null;
    }
}
