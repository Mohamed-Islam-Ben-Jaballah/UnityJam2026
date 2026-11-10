using System;
using DiceDrive.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDrive.UI
{
    /// <summary>
    /// Shows one finished card image and reports pointer input. It does not know how many
    /// cards exist or where it belongs: its owner sets a target pose and it eases toward it.
    /// The root RectTransform holds the slot position/rotation; the <c>visual</c> child carries
    /// lift/scale and is the hit area, so a card peeking from the screen edge stays hovered
    /// while it rises into view under the pointer.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private Image artwork;
        [Tooltip("Tint applied to the card image when it is shaded (another card is hovered).")]
        [SerializeField] private Color shadedTint = new(0.4f, 0.4f, 0.45f);
        [SerializeField] private float smoothing = 14f;
        [Tooltip("How long the face-down to face-up flip takes after a dealt card lands.")]
        [SerializeField] private float flipDuration = 0.25f;
        [Tooltip("A dealt card starts flipping once it is this close to its slot.")]
        [SerializeField] private float flipStartDistance = 6f;

        public CardInfo Card { get; private set; }

        public event Action<CardView> PointerEntered;
        public event Action<CardView> PointerExited;
        public event Action<CardView> Clicked;
        /// <summary>Raised once when a dealt card has landed and turned face-up.</summary>
        public event Action<CardView> DealFinished;

        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 targetPosition;
        private float targetAngle;
        private float targetLift;
        private float targetScale = 1f;
        private float scale = 1f;
        private float targetShade;
        private float shade;
        private float targetAlpha = 1f;
        private bool dismissing;

        // Dealing: wait at the deck, fly face-down, then flip to the face.
        private float dealDelay;
        private bool dealing; // true until the card leaves the deck (plays the draw sound once)
        private bool faceDown;
        private float flip = -1f; // < 0: not flipping; 0..1: flip progress

        private void Awake()
        {
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
        }

        public void Bind(CardInfo card)
        {
            Card = card;
            if (!faceDown) ShowFace();
        }

        /// <summary>
        /// Deals the card from <paramref name="from"/> (e.g. the deck): it stays hidden for
        /// <paramref name="delay"/> seconds, flies to its slot showing <paramref name="back"/>,
        /// then flips to its face. Not clickable until the flip is done.
        /// </summary>
        public void Deal(Vector2 from, float startScale, float delay, Sprite back)
        {
            dealing = true;
            rect.anchoredPosition = from;
            rect.localRotation = Quaternion.identity;
            scale = startScale;
            visual.localScale = Vector3.one * startScale;
            dealDelay = delay;
            group.alpha = 0f;

            if (back == null) return;
            faceDown = true;
            flip = -1f;
            artwork.sprite = back;
            artwork.enabled = true;
            SetInteractable(false);
        }

        private void ShowFace()
        {
            artwork.sprite = Card.Artwork;
            artwork.enabled = Card.Artwork != null;
        }

        /// <summary>Sets the pose the card eases toward. <paramref name="shadeAmount"/> is 0 (normal) to 1 (fully shaded).</summary>
        public void SetPose(Vector2 position, float angle, float lift, float scale, float shadeAmount)
        {
            targetPosition = position;
            targetAngle = angle;
            targetLift = lift;
            targetScale = scale;
            targetShade = shadeAmount;
        }

        /// <summary>Places the card immediately (e.g. to deal it in from off-screen).</summary>
        public void JumpTo(Vector2 position) => rect.anchoredPosition = position;

        public void SetInteractable(bool interactable) => group.blocksRaycasts = interactable;

        /// <summary>Fades the card out, ignores further input, then destroys it.</summary>
        public void Dismiss()
        {
            dismissing = true;
            SetInteractable(false);
            targetAlpha = 0f;
        }

        private void Update()
        {
            // Capped so a hitch (e.g. FMOD loading a sound on first play) can't skip the deal:
            // without it every remaining delay expires in one frame and the cards snap into place.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            if (dealDelay > 0f)
            {
                dealDelay -= dt; // waiting for its turn at the deck
                group.alpha = 0f;
                return;
            }

            if (dealing)
            {
                dealing = false; // this card is leaving the deck right now
                PlayDrawSound();
                if (!faceDown) DealFinished?.Invoke(this); // no card back: there is no flip to wait for
            }

            float t = 1f - Mathf.Exp(-smoothing * dt);

            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, t);
            rect.localRotation = Quaternion.Slerp(rect.localRotation, Quaternion.Euler(0f, 0f, targetAngle), t);
            visual.anchoredPosition = Vector2.Lerp(visual.anchoredPosition, new Vector2(0f, targetLift), t);
            scale = Mathf.Lerp(scale, targetScale, t);
            visual.localScale = new Vector3(scale * UpdateFlip(dt), scale, 1f);

            shade = Mathf.Lerp(shade, targetShade, t);
            artwork.color = Color.Lerp(Color.white, shadedTint, shade);

            group.alpha = Mathf.Lerp(group.alpha, targetAlpha, t);
            if (dismissing && group.alpha < 0.02f) Destroy(gameObject);
        }

        private static void PlayDrawSound()
        {
            // Skipped in scenes without the audio objects (e.g. QA scenes).
            if (AudioManager.instance == null || FMODEvents.instance == null) return;
            AudioManager.instance.PlayOneShot(FMODEvents.instance.drawCard);
        }

        /// <summary>Advances the deal flip and returns the horizontal scale factor (1 = flat, 0 = edge-on).</summary>
        private float UpdateFlip(float dt)
        {
            if (!faceDown) return 1f;

            if (flip < 0f)
            {
                if ((rect.anchoredPosition - targetPosition).sqrMagnitude > flipStartDistance * flipStartDistance) return 1f;
                flip = 0f; // landed: start flipping
            }

            flip = Mathf.Min(flip + dt / flipDuration, 1f);
            if (flip >= 0.5f && artwork.sprite != Card.Artwork) ShowFace(); // swap at the edge-on moment

            if (flip < 1f) return Mathf.Abs(Mathf.Cos(flip * Mathf.PI));

            faceDown = false;
            flip = -1f;
            if (!dismissing) SetInteractable(true);
            DealFinished?.Invoke(this);
            return 1f;
        }

        public void OnPointerEnter(PointerEventData eventData) => PointerEntered?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => PointerExited?.Invoke(this);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
        }
    }
}
