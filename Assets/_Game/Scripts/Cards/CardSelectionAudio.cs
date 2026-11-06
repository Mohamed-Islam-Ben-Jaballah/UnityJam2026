using FMODUnity;
using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>
    /// Plays an FMOD event whenever a card is selected. Assign the event in the Inspector
    /// once it exists in the FMOD Studio project; while unassigned it stays silent.
    /// </summary>
    public sealed class CardSelectionAudio : MonoBehaviour
    {
        [SerializeField] private CardSelectionService service;
        [SerializeField] private EventReference cardSelectedSound;

        private void OnEnable() => service.Selection.CardSelected += OnCardSelected;
        private void OnDisable() => service.Selection.CardSelected -= OnCardSelected;

        private void OnCardSelected(CardInfo card)
        {
            if (!cardSelectedSound.IsNull) RuntimeManager.PlayOneShot(cardSelectedSound);
        }
    }
}
