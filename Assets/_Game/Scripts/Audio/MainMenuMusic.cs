using FMOD.Studio;
using UnityEngine;
using UnityEngine.UI;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays the main menu theme for as long as the menu is open. Looping and the AHDSR fade-out
    /// live in the FMOD event, so this only starts it and stops it with ALLOWFADEOUT, which lets
    /// FMOD run the release. The theme fades out when the Start button is clicked (if one is
    /// assigned), when <see cref="FadeOut"/> is called, or when this object is destroyed.
    /// </summary>
    public sealed class MainMenuMusic : MonoBehaviour
    {
        [Tooltip("Optional. Clicking this button fades the theme out. Leave empty and call FadeOut() yourself if you prefer.")]
        [SerializeField] private Button startButton;

        private EventInstance music;
        private bool playing;

        private void OnEnable()
        {
            if (startButton != null) startButton.onClick.AddListener(FadeOut);
        }

        private void OnDisable()
        {
            if (startButton != null) startButton.onClick.RemoveListener(FadeOut);
        }

        private void Start()
        {
            if (!AudioReady) return;

            music = AudioManager.instance.CreateInstance(FMODEvents.instance.MainMenuMusic);
            if (!music.isValid()) return;

            music.start();
            playing = true;
        }

        /// <summary>Fades the theme out using the FMOD release. Safe to call more than once.</summary>
        public void FadeOut()
        {
            if (!playing) return;
            playing = false;

            music.stop(STOP_MODE.ALLOWFADEOUT);
            // FMOD keeps the instance alive until the fade finishes, then frees it,
            // so the fade carries on even if the next scene loads straight away.
            music.release();
        }

        private void OnDestroy() => FadeOut();

        // False in scenes without the audio objects, which then stay silent.
        private static bool AudioReady => AudioManager.instance != null && FMODEvents.instance != null;
    }
}
