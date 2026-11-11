using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays a scene's background music. Put one in each scene that needs music and pick the track
    /// in the Inspector. Looping and the AHDSR fade in/out live in the FMOD event, so this only
    /// starts and stops it (ALLOWFADEOUT lets FMOD run the release).
    /// From code: <see cref="Play"/> switches to another track (the current one fades out),
    /// <see cref="FadeOut"/> fades the music out, <see cref="Stop"/> cuts it immediately.
    /// The music also fades out when the scene unloads.
    /// </summary>
    public sealed class SceneMusic : MonoBehaviour
    {
        [Tooltip("Track that starts with the scene. Leave empty for no music at start.")]
        [SerializeField] private EventReference music;

        private EventInstance current;
        private bool playing;

        private void Start()
        {
            if (!music.IsNull) Play(music);
        }

        /// <summary>Fades out whatever is playing and starts <paramref name="track"/>.</summary>
        public void Play(EventReference track)
        {
            FadeOut();
            if (AudioManager.instance == null) return;

            current = AudioManager.instance.CreateInstance(track);
            if (!current.isValid()) return;

            current.start();
            playing = true;
        }

        /// <summary>Fades the music out using the FMOD release. Safe to call more than once.</summary>
        public void FadeOut() => StopMusic(STOP_MODE.ALLOWFADEOUT);

        /// <summary>Stops the music immediately, without the fade.</summary>
        public void Stop() => StopMusic(STOP_MODE.IMMEDIATE);

        private void StopMusic(STOP_MODE mode)
        {
            if (!playing) return;
            playing = false;

            current.stop(mode);
            // FMOD keeps the instance alive until the fade finishes, then frees it,
            // so the fade carries on even if the next scene loads straight away.
            current.release();
        }

        private void OnDestroy() => FadeOut();
    }
}
