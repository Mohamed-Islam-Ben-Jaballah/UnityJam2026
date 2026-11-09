using System;
using System.Collections.Generic;

namespace DiceDrive.Cards
{
    public sealed class Deck
    {
        private readonly List<CardInstance> _cards;

        public int Count => _cards.Count;
        public IReadOnlyList<CardInstance> Cards => _cards;
        public event Action DeckExhausted;

        public Deck(IEnumerable<CardInstance> cards)
        {
            _cards = new List<CardInstance>(cards ?? throw new ArgumentNullException(nameof(cards)));
        }

        public void Shuffle(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public bool TryDraw(out CardInstance card)
        {
            if (_cards.Count == 0)
            {
                card = null;
                DeckExhausted?.Invoke();
                return false;
            }

            card = _cards[0];
            _cards.RemoveAt(0);
            return true;
        }

        public void Add(CardInstance card)
        {
            if (card != null)
            {
                _cards.Add(card);
            }
        }

        public void Clear()
        {
            _cards.Clear();
        }
    }
}
