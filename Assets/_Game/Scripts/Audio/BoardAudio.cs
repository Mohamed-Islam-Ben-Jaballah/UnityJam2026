using DiceDrive.Board;
using UnityEngine;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays the board sounds by listening to <see cref="DiceBoardMovement"/>; the movement script
    /// itself has no audio code. Currently: the illegal move sound when the player clicks a tile
    /// that doesn't match the dice roll.
    /// </summary>
    [RequireComponent(typeof(DiceBoardMovement))]
    public sealed class BoardAudio : MonoBehaviour
    {
        private DiceBoardMovement movement;

        private void Awake() => movement = GetComponent<DiceBoardMovement>();

        private void OnEnable() => movement.IllegalMoveAttempted += OnIllegalMove;

        private void OnDisable() => movement.IllegalMoveAttempted -= OnIllegalMove;

        private void Start()
        {
            // Load the sound up front so the first wrong click doesn't stall while FMOD reads it from disk.
            if (!AudioReady) return;
            AudioManager.instance.LoadSampleData(FMODEvents.instance.illegalMove);
        }

        private void OnIllegalMove(int tile)
        {
            if (AudioReady) AudioManager.instance.PlayOneShot(FMODEvents.instance.illegalMove);
        }

        // False in scenes without the audio objects, which then stay silent.
        private static bool AudioReady => AudioManager.instance != null && FMODEvents.instance != null;
    }
}
