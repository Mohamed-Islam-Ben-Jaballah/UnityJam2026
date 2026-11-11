using DiceDrive.UI;
using UnityEngine;

namespace DiceDrive.Audio
{
    /// <summary>
    /// Plays the hand's card sounds (hover, each card dealt, deal finished) by listening to
    /// <see cref="HandView"/>; the hand and card views themselves have no audio code.
    /// </summary>
    [RequireComponent(typeof(HandView))]
    public sealed class HandAudio : MonoBehaviour
    {
        private HandView hand;

        private void Awake() => hand = GetComponent<HandView>();

        private void OnEnable()
        {
            hand.CardHovered += OnCardHovered;
            hand.CardDealt += OnCardDealt;
            hand.DealCompleted += OnDealCompleted;
        }

        private void OnDisable()
        {
            hand.CardHovered -= OnCardHovered;
            hand.CardDealt -= OnCardDealt;
            hand.DealCompleted -= OnDealCompleted;
        }

        private void Start()
        {
            // Load the card sounds up front so the first deal or hover doesn't stall while FMOD reads them from disk.
            if (!AudioReady) return;
            AudioManager.instance.LoadSampleData(FMODEvents.instance.drawCard);
            AudioManager.instance.LoadSampleData(FMODEvents.instance.drawCardEnd);
            AudioManager.instance.LoadSampleData(FMODEvents.instance.HoverCard);
        }

        private void OnCardHovered(CardView card)
        {
            if (AudioReady) AudioManager.instance.PlayOneShot(FMODEvents.instance.HoverCard);
        }

        private void OnCardDealt(CardView card)
        {
            if (AudioReady) AudioManager.instance.PlayOneShot(FMODEvents.instance.drawCard);
        }

        private void OnDealCompleted()
        {
            if (AudioReady) AudioManager.instance.PlayOneShot(FMODEvents.instance.drawCardEnd);
        }

        // False in scenes without the audio objects (e.g. QA scenes), which then stay silent.
        private static bool AudioReady => AudioManager.instance != null && FMODEvents.instance != null;
    }
}
