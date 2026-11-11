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
        [Tooltip("Optional text shown only while the card has no artwork (placeholder cards).")]
        [SerializeField] private TMPro.TMP_Text placeholderLabel;
        [Tooltip("Tint while the card can't be played (e.g. dice don't match).")]
        [SerializeField] private Color lockedTint = new(0.35f, 0.35f, 0.38f);
        [Tooltip("Tint applied to the card image when it is shaded (another card is hovered).")]
        [SerializeField] private Color shadedTint = new(0.4f, 0.4f, 0.45f);
        [SerializeField] private float smoothing = 14f;

        public CardInfo Card { get; private set; }

        public event Action<CardView> PointerEntered;
        public event Action<CardView> PointerExited;
        public event Action<CardView> Clicked;

        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 targetPosition;
        private float targetAngle;
        private float targetLift;
        private float targetScale = 1f;
        private float targetShade;
        private float shade;
        private float targetAlpha = 1f;
        private bool dismissing;
        private Color baseColor = Color.white;
        private bool locked;

        private void Awake()
        {
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
        }

        public void Bind(CardInfo card)
        {
            Card = card;
            artwork.sprite = card.Artwork;

            // No artwork yet: show a plain coloured card with its label instead of nothing.
            var placeholder = card.Artwork == null && !string.IsNullOrEmpty(card.Label);
            artwork.enabled = card.Artwork != null || placeholder;
            baseColor = card.Artwork != null ? Color.white : card.PlaceholderColor;
            if (placeholderLabel != null)
            {
                placeholderLabel.gameObject.SetActive(placeholder);
                placeholderLabel.text = card.Label;
            }
        }

        /// <summary>Locked cards are dimmed (they can still be hovered).</summary>
        public void SetLocked(bool value) => locked = value;

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
            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);

            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, t);
            rect.localRotation = Quaternion.Slerp(rect.localRotation, Quaternion.Euler(0f, 0f, targetAngle), t);
            visual.anchoredPosition = Vector2.Lerp(visual.anchoredPosition, new Vector2(0f, targetLift), t);
            visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one * targetScale, t);

            shade = Mathf.Lerp(shade, targetShade, t);
            var tinted = baseColor * (locked ? lockedTint : Color.white);
            artwork.color = Color.Lerp(tinted, tinted * shadedTint, shade);

            group.alpha = Mathf.Lerp(group.alpha, targetAlpha, t);
            if (dismissing && group.alpha < 0.02f) Destroy(gameObject);
        }

        public void OnPointerEnter(PointerEventData eventData) => PointerEntered?.Invoke(this);
        public void OnPointerExit(PointerEventData eventData) => PointerExited?.Invoke(this);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke(this);
        }
    }
}
