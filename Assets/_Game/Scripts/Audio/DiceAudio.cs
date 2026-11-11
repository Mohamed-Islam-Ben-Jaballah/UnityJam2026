using DiceDrive.UI;
using FMOD.Studio;
using UnityEngine;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays the dice sounds by listening to <see cref="DiceView"/>; the view itself has no audio code.
    /// Lives on the same GameObject as the die, so hiding the die mid-roll also fades its sound out.
    /// </summary>
    [RequireComponent(typeof(DiceView))]
    public sealed class DiceAudio : MonoBehaviour
    {
        private DiceView dice;
        private EventInstance rollSound;

        private void Awake() => dice = GetComponent<DiceView>();

        private void OnEnable() => dice.RollStarted += OnRollStarted;

        private void OnDisable()
        {
            dice.RollStarted -= OnRollStarted;
            StopRollSound(STOP_MODE.ALLOWFADEOUT);
        }

        private void Start()
        {
            // Load the roll sound up front so the first roll doesn't stall while FMOD reads it from disk.
            if (AudioManager.instance == null || FMODEvents.instance == null) return;
            AudioManager.instance.LoadSampleData(FMODEvents.instance.diceRoll);
        }

        private void OnRollStarted()
        {
            // The DiceRoll event allows one instance at a time, so a quick re-roll would be
            // silenced: stop the previous sound before starting the new one.
            StopRollSound(STOP_MODE.IMMEDIATE);
            if (AudioManager.instance == null || FMODEvents.instance == null) return;

            rollSound = AudioManager.instance.CreateInstance(FMODEvents.instance.diceRoll);
            if (!rollSound.isValid()) return;
            rollSound.start();
            rollSound.release(); // freed by FMOD once it finishes playing
        }

        private void StopRollSound(STOP_MODE mode)
        {
            if (rollSound.isValid()) rollSound.stop(mode);
        }
    }
}
