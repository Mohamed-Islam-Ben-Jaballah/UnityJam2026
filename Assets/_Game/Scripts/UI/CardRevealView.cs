using System.Collections;
using UnityEngine;

namespace DiceDrive.UI
{
    /// <summary>
    /// Presents a just-selected card: darkens the screen, moves the card to the center,
    /// plays the reveal animation, then clears. The backdrop blocks input while it runs.
    /// </summary>
    public sealed class CardRevealView : MonoBehaviour
    {
        [Tooltip("Full-screen layer above the hand; the selected card is moved here (center = anchored 0,0).")]
        [SerializeField] private RectTransform stage;
        [Tooltip("Full-screen dark overlay. Hidden until a card is chosen.")]
        [SerializeField] private CanvasGroup backdrop;
        [SerializeField] private float revealScale = 1.8f;
        [SerializeField] private float backdropFadeIn = 0.25f;
        [SerializeField] private float backdropFadeOut = 0.3f;
        [Tooltip("How long the placeholder reveal holds the card on screen.")]
        [SerializeField] private float holdDuration = 1.2f;

        private Coroutine running;
        private CardView current;

        private void Awake()
        {
            backdrop.alpha = 0f;
            backdrop.blocksRaycasts = false;
        }

        public void Play(CardView card)
        {
            if (running != null)
            {
                StopCoroutine(running);
                current.Dismiss();
            }
            running = StartCoroutine(Reveal(card));
        }

        private IEnumerator Reveal(CardView card)
        {
            current = card;
            card.SetInteractable(false);
            card.transform.SetParent(stage, worldPositionStays: true);
            card.SetPose(Vector2.zero, 0f, 0f, revealScale, 0f);

            backdrop.blocksRaycasts = true;
            yield return FadeBackdrop(1f, backdropFadeIn);

            yield return PlayRevealAnimation(card);

            card.Dismiss();
            yield return FadeBackdrop(0f, backdropFadeOut);
            backdrop.blocksRaycasts = false;

            current = null;
            running = null;
        }

        /// <summary>
        /// ANIMATION SLOT — the "K.O."-style moment. The card is already centered and the screen
        /// darkened when this starts; when it finishes, the card fades out and the screen clears.
        /// Replace the hold below with the real animation (Animator trigger, Timeline, VFX, FMOD stinger).
        /// </summary>
        private IEnumerator PlayRevealAnimation(CardView card)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        private IEnumerator FadeBackdrop(float to, float duration)
        {
            float from = backdrop.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                backdrop.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            backdrop.alpha = to;
        }
    }
}
