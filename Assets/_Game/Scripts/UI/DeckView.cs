using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDrive.UI
{
    /// <summary>
    /// The deck pile. It never draws or chooses cards: gameplay switches it on with
    /// <see cref="SetDrawAllowed"/>, and a click while allowed raises <see cref="DrawRequested"/>.
    /// Allowed: glows, pulses, reacts to hover. Not allowed: greyed out and ignores input.
    /// Each permission is good for one request; the deck switches itself off after a click
    /// so a double click can't ask twice. Call SetDrawAllowed(true) again to allow another draw.
    /// </summary>
    public sealed class DeckView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Image that receives clicks (the top card of the pile).")]
        [SerializeField] private Graphic hitArea;
        [Tooltip("Images greyed out while drawing is not allowed.")]
        [SerializeField] private Graphic[] tinted;
        [Tooltip("Outline shown (and pulsed) while drawing is allowed.")]
        [SerializeField] private Outline glow;
        [SerializeField] private Color glowColor = new(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color disabledTint = new(0.5f, 0.5f, 0.55f, 1f);
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float pulseScale = 0.05f;
        [SerializeField] private float hoverScale = 0.08f;

        /// <summary>Raised when the player clicks the deck while drawing is allowed.</summary>
        public event Action DrawRequested;

        public bool DrawAllowed { get; private set; }

        private Color[] baseColors;
        private bool hovered;

        private void Awake()
        {
            baseColors = new Color[tinted.Length];
            for (int i = 0; i < tinted.Length; i++) baseColors[i] = tinted[i].color;
            Apply();
        }

        public void SetDrawAllowed(bool allowed)
        {
            DrawAllowed = allowed;
            if (!allowed) hovered = false;
            Apply();
        }

        private void Apply()
        {
            if (baseColors == null) return; // before Awake; Awake applies the current state
            hitArea.raycastTarget = DrawAllowed;
            glow.enabled = DrawAllowed;
            for (int i = 0; i < tinted.Length; i++)
                tinted[i].color = DrawAllowed ? baseColors[i] : baseColors[i] * disabledTint;
            if (!DrawAllowed) transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!DrawAllowed) return;

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            transform.localScale = Vector3.one * (1f + pulseScale * wave + (hovered ? hoverScale : 0f));
            var color = glowColor;
            color.a *= Mathf.Lerp(0.35f, 1f, wave);
            glow.effectColor = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!DrawAllowed || eventData.button != PointerEventData.InputButton.Left) return;
            SetDrawAllowed(false);
            DrawRequested?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = DrawAllowed;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;
    }
}
