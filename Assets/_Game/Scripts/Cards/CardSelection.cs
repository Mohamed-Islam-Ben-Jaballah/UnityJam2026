using System;
using System.Collections.Generic;

namespace DiceDrive.Cards
{
    /// <summary>
    /// Source of truth for card selection. Plain C# (no MonoBehaviour) so it outlives any UI
    /// rebuild and can be unit-tested. Views only read it and react to its events.
    /// </summary>
    public sealed class CardSelection
    {
        private readonly List<CardInfo> offered = new();
        private readonly List<CardInfo> selected = new();
        private readonly HashSet<string> selectedIds = new();

        /// <summary>Raised after a card moves from the offer into the selection.</summary>
        public event Action<CardInfo> CardSelected;

        /// <summary>Raised whenever the set of offered cards changes (new offer or a selection).</summary>
        public event Action OfferChanged;

        /// <summary>Cards currently available to pick, in display order.</summary>
        public IReadOnlyList<CardInfo> Offered => offered;

        /// <summary>Cards picked so far, in the order they were picked.</summary>
        public IReadOnlyList<CardInfo> Selected => selected;

        public bool IsSelected(string cardId) => cardId != null && selectedIds.Contains(cardId);

        /// <summary>
        /// Replaces the offered cards with any number of cards. Cards already selected and
        /// duplicate ids are skipped, so a card can never be offered (or picked) twice.
        /// </summary>
        public void SetOffer(IEnumerable<CardInfo> cards)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            offered.Clear();
            var seen = new HashSet<string>();
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (string.IsNullOrEmpty(card.Id))
                    throw new ArgumentException("A card has no Id; every card needs a unique Id.", nameof(cards));
                if (selectedIds.Contains(card.Id) || !seen.Add(card.Id)) continue;
                offered.Add(card);
            }

            OfferChanged?.Invoke();
        }

        /// <summary>
        /// Picks an offered card by id. Returns false if it is not offered or was already picked.
        /// </summary>
        public bool TrySelect(string cardId)
        {
            if (cardId == null || selectedIds.Contains(cardId)) return false;

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
