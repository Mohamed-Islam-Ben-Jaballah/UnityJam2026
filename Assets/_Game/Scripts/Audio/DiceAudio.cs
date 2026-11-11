using DiceDrive.UI;
using FMOD.Studio;
using UnityEngine;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays the dice sounds by listening to <see cref="DiceView"/>; the view itself has no audio code.
    /// Lives on the same GameObject as the die, so hiding the die mid-roll also fades its sound out.
    /// When the die lands on 2, 5 or 6 it plays the elf, devil or chinese chime.
    /// </summary>
    [RequireComponent(typeof(DiceView))]
    public sealed class DiceAudio : MonoBehaviour
    {
        private DiceView dice;
        private EventInstance rollSound;

        private void Awake() => dice = GetComponent<DiceView>();

        private void OnEnable()
        {
            dice.RollStarted += OnRollStarted;
            dice.Landed += OnLanded;
        }

        private void OnDisable()
        {
            dice.RollStarted -= OnRollStarted;
            dice.Landed -= OnLanded;
            StopRollSound(STOP_MODE.ALLOWFADEOUT);
        }

        private void Start()
        {
            // Load the sounds up front so the first roll doesn't stall while FMOD reads them from disk.
            if (AudioManager.instance == null || FMODEvents.instance == null) return;
            AudioManager.instance.LoadSampleData(FMODEvents.instance.diceRoll);
            AudioManager.instance.LoadSampleData(FMODEvents.instance.elfCharm);
            AudioManager.instance.LoadSampleData(FMODEvents.instance.devilCharm);
            AudioManager.instance.LoadSampleData(FMODEvents.instance.chineseCharm);
        }

        private void OnLanded(int value)
        {
            if (AudioManager.instance == null || FMODEvents.instance == null) return;

            if (value == 2) AudioManager.instance.PlayOneShot(FMODEvents.instance.elfCharm);
            else if (value == 5) AudioManager.instance.PlayOneShot(FMODEvents.instance.devilCharm);
            else if (value == 6) AudioManager.instance.PlayOneShot(FMODEvents.instance.chineseCharm);
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
