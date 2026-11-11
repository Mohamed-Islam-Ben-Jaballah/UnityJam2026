using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays the UI hover sound when the mouse moves onto the button and the UI click sound when it
    /// is clicked. Add it to any Button (Start, Exit, ...); the button needs no audio code itself.
    /// Disabled (non-interactable) buttons stay silent.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonAudio : MonoBehaviour, IPointerEnterHandler
    {
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable() => button.onClick.AddListener(OnClick);

        private void OnDisable() => button.onClick.RemoveListener(OnClick);

        private void Start()
        {
            // Load the sounds up front so the first hover or click doesn't stall while FMOD reads them from disk.
            if (!AudioReady) return;
            AudioManager.instance.LoadSampleData(FMODEvents.instance.ButtonHover);
            AudioManager.instance.LoadSampleData(FMODEvents.instance.ButtonClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.IsInteractable() && AudioReady)
                AudioManager.instance.PlayOneShot(FMODEvents.instance.ButtonHover);
        }

        private void OnClick()
        {
            if (AudioReady) AudioManager.instance.PlayOneShot(FMODEvents.instance.ButtonClick);
        }

        // False in scenes without the audio objects, which then stay silent.
        private static bool AudioReady => AudioManager.instance != null && FMODEvents.instance != null;
    }
}
