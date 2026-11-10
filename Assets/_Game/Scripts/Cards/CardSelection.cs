using System;
using System.Collections.Generic;

namespace DiceDrive.Cards
{
    /// <summary>
    /// UI state of the hand: which cards are shown (offered) and which the player picked.
    /// Plain C# (no MonoBehaviour) so it outlives any UI rebuild and can be unit-tested.
    /// Views only read it and react to its events. It never decides game rules: whoever owns the
    /// rules feeds it cards and switches <see cref="SelectionEnabled"/>.
    /// </summary>
    public sealed class CardSelection
    {
        private readonly List<CardInfo> offered = new();
        private readonly List<CardInfo> selected = new();
        private readonly HashSet<string> selectedIds = new();

        /// <summary>Raised after a card moves from the offer into the selection.</summary>
        public event Action<CardInfo> CardSelected;

        /// <summary>Raised whenever the set of offered cards changes (new offer, card added/removed, or a selection).</summary>
        public event Action OfferChanged;

        /// <summary>Cards currently shown in the hand, in display order.</summary>
        public IReadOnlyList<CardInfo> Offered => offered;

        /// <summary>Cards picked so far, in the order they were picked.</summary>
        public IReadOnlyList<CardInfo> Selected => selected;

        public bool IsSelected(string cardId) => cardId != null && selectedIds.Contains(cardId);

        /// <summary>
        /// When false, <see cref="TrySelect"/> refuses every pick. The hand stays visible and
        /// hoverable; only selecting is blocked.
        /// </summary>
        public bool SelectionEnabled { get; set; } = true;

        /// <summary>
        /// Replaces the shown cards with any number of cards. Cards already selected and duplicate
        /// ids are skipped, so a card can never be shown (or picked) twice. Nothing changes if the
        /// list contains a card without an id.
        /// </summary>
        public void SetOffer(IEnumerable<CardInfo> cards)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            // Validate everything first so a bad list never leaves the hand half-updated.
            var next = new List<CardInfo>();
            var seen = new HashSet<string>();
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (string.IsNullOrEmpty(card.Id))
                    throw new ArgumentException("A card has no Id; every card needs a unique Id.", nameof(cards));
                if (selectedIds.Contains(card.Id) || !seen.Add(card.Id)) continue;
                next.Add(card);
            }

            offered.Clear();
            offered.AddRange(next);
            OfferChanged?.Invoke();
        }

        /// <summary>
        /// Adds one card to the end of the hand (e.g. a card just drawn). Returns false if a card with
        /// the same id is already shown or was already picked.
        /// </summary>
        public bool AddToOffer(CardInfo card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (string.IsNullOrEmpty(card.Id)) throw new ArgumentException("The card has no Id.", nameof(card));
            if (selectedIds.Contains(card.Id) || offered.Exists(c => c.Id == card.Id)) return false;

            offered.Add(card);
            OfferChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Removes a shown card without selecting it (e.g. discarded because the hand is full).
        /// Returns false if no card with that id is shown.
        /// </summary>
        public bool Withdraw(string cardId)
        {
            int index = cardId == null ? -1 : offered.FindIndex(c => c.Id == cardId);
            if (index < 0) return false;

            offered.RemoveAt(index);
            OfferChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Picks a shown card by id. Returns false if selection is disabled, or the card is not
        /// shown or was already picked.
        /// </summary>
        public bool TrySelect(string cardId)
        {
            if (!SelectionEnabled || cardId == null || selectedIds.Contains(cardId)) return false;

            int index = offered.FindIndex(c => c.Id == cardId);
            if (index < 0) return false;

            var card = offered[index];
            offered.RemoveAt(index);
            selected.Add(card);
            selectedIds.Add(cardId);

            CardSelected?.Invoke(card);
            OfferChanged?.Invoke();
            return true;
        }
    }
}
