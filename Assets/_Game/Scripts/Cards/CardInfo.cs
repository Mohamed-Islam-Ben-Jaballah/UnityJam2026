using System;
using UnityEngine;

namespace DiceDrive.Cards
{
    /// <summary>
    /// What the card UI shows for one card: a unique id plus the finished card image (the designer's
    /// art already contains the text, icons and values). Holds no UI or gameplay logic.
    /// Build one from gameplay data with <see cref="From(CardInstance)"/>.
    /// </summary>
    [Serializable]
    public sealed class CardInfo
    {
        [Tooltip("Unique, stable identifier. Selection is tracked by this value.")]
        [SerializeField] private string id;
        [Tooltip("The complete, finished card image.")]
        [SerializeField] private Sprite artwork;

        [NonSerialized] private CardInstance instance;

        public CardInfo(string id, Sprite artwork)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A card needs a unique, non-empty id.", nameof(id));
            this.id = id;
            this.artwork = artwork;
        }

        public string Id => id;
        public Sprite Artwork => artwork;

        /// <summary>
        /// The gameplay card this was built from (null for placeholder cards). Lets the gameplay side
        /// get its own <see cref="CardInstance"/> straight back from a UI event.
        /// </summary>
        public CardInstance Instance => instance;

        /// <summary>UI card for a gameplay card copy: id = <see cref="CardInstance.Id"/>, image = <see cref="CardData.art"/>.</summary>
        public static CardInfo From(CardInstance card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            return new CardInfo(card.Id.ToString(), card.Data != null ? card.Data.art : null) { instance = card };
        }

        /// <summary>UI card straight from a card asset (previews and tests, before gameplay is connected).</summary>
        public static CardInfo From(CardData data, string id)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new CardInfo(id, data.art);
        }
    }
}
