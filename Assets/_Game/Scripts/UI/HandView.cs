using System.Collections.Generic;
using DiceDrive.Cards;
using FMODUnity;
using UnityEngine;

namespace DiceDrive.UI
{
    /// <summary>
    /// Mirrors <see cref="CardSelection.Offered"/> as a row of <see cref="CardView"/>s:
    /// creates, keeps or dismisses views whenever the offer changes, arranges them with
    /// <see cref="HandLayout"/>, and forwards clicks as selection requests.
    /// Holds no selection state of its own, so rebuilding it never loses a pick.
    /// </summary>
    public sealed class HandView : MonoBehaviour
    {
        [SerializeField] private CardSelectionService service;
        [SerializeField] private CardView cardPrefab;
        [SerializeField] private RectTransform cardContainer;
        [Tooltip("Presents the selected card. If empty, the selected card just fades out.")]
        [SerializeField] private CardRevealView reveal;
        [Header("Dealing")]
        [Tooltip("Deck pile new cards are dealt from. If empty, cards slide up from below instead.")]
        [SerializeField] private RectTransform deck;
        [Tooltip("Card back shown while a card flies from the deck, before it flips.")]
        [SerializeField] private Sprite cardBack;
        [Tooltip("Seconds between one card leaving the deck and the next.")]
        [SerializeField] private float dealInterval = 0.15f;
        [Tooltip("Used only when no deck is set.")]
        [SerializeField] private float dealInDistance = 500f;
        [SerializeField] private HandLayout layout = new();

        private readonly List<CardView> views = new();
        private CardView hovered;

        private void OnEnable()
        {
            service.Selection.OfferChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            service.Selection.OfferChanged -= Refresh;
        }

        private void Start()
        {
            // Load the deal sounds up front so the first card doesn't stall while FMOD reads them from disk.
            if (FMODEvents.instance == null) return;
            PreloadSound(FMODEvents.instance.drawCard);
            PreloadSound(FMODEvents.instance.drawCardEnd);
        }

        private static void PreloadSound(EventReference sound)
        {
            if (!sound.IsNull) RuntimeManager.GetEventDescription(sound).loadSampleData();
        }

        private void Refresh()
        {
            var offered = service.Selection.Offered;

            var current = new Dictionary<string, CardView>(views.Count);
            foreach (var view in views) current[view.Card.Id] = view;

            views.Clear();
            var created = new List<CardView>();
            foreach (var card in offered)
            {
                if (current.Remove(card.Id, out var view))
                {
                    view.Bind(card);
                }
                else
                {
                    view = Instantiate(cardPrefab, cardContainer);
                    view.Bind(card);
                    view.PointerEntered += OnPointerEntered;
                    view.PointerExited += OnPointerExited;
                    view.Clicked += OnClicked;
                    created.Add(view);
                }
                views.Add(view);
            }

            // Whatever is left was picked (goes to the reveal) or withdrawn from the offer (fades out).
            foreach (var removed in current.Values)
            {
                removed.PointerEntered -= OnPointerEntered;
                removed.PointerExited -= OnPointerExited;
                removed.Clicked -= OnClicked;
                if (removed == hovered) hovered = null;

                if (reveal != null && service.Selection.IsSelected(removed.Card.Id)) reveal.Play(removed);
                else removed.Dismiss();
            }

            ApplyLayout();
            Deal(created);
        }

        /// <summary>New cards leave the deck one after another, face-down, and flip on landing.</summary>
        private void Deal(List<CardView> created)
        {
            if (deck == null)
            {
                foreach (var view in created)
                {
                    layout.GetSlot(views.IndexOf(view), views.Count, -1, out var slot, out _);
                    view.JumpTo(slot + Vector2.down * dealInDistance);
                }
                return;
            }

            // Cards sit centered in the container, so its local space is their anchored space.
            Vector2 origin = cardContainer.InverseTransformPoint(deck.position);
            for (int i = 0; i < created.Count; i++)
            {
                var view = created[i];
                float startScale = deck.rect.width / ((RectTransform)view.transform).rect.width;
                view.Deal(origin, startScale, i * dealInterval, cardBack);
            }

            // The last card is dealt last, so once it is face-up the whole deal is done.
            if (created.Count > 0) created[created.Count - 1].DealFinished += OnLastCardDealt;
        }

        private void OnLastCardDealt(CardView last)
        {
            last.DealFinished -= OnLastCardDealt;
            if (AudioManager.instance == null || FMODEvents.instance == null) return;
            AudioManager.instance.PlayOneShot(FMODEvents.instance.drawCardEnd);
        }

        private void ApplyLayout()
        {
            int hoveredIndex = hovered != null ? views.IndexOf(hovered) : -1;

            for (int i = 0; i < views.Count; i++)
            {
                layout.GetSlot(i, views.Count, hoveredIndex, out var position, out float angle);

                bool isHovered = i == hoveredIndex;
                bool isDimmed = hoveredIndex >= 0 && !isHovered;

                views[i].SetPose(
                    position,
                    isHovered ? 0f : angle,
                    isHovered ? layout.hoverLift : 0f,
                    isHovered ? layout.hoverScale : isDimmed ? layout.dimmedScale : 1f,
                    isDimmed ? layout.shadeAmount : 0f);
                views[i].transform.SetSiblingIndex(i);
            }

            if (hovered != null) hovered.transform.SetAsLastSibling();
        }

        private void OnPointerEntered(CardView view)
        {
            hovered = view;
            ApplyLayout();
        }

        private void OnPointerExited(CardView view)
        {
            if (hovered != view) return;
            hovered = null;
            ApplyLayout();
        }

        private void OnClicked(CardView view)
        {
            service.Selection.TrySelect(view.Card.Id);
        }
    }
}
