using System;
using System.Collections.Generic;

namespace DiceDrive.Cards
{
    public sealed class Hand
    {
        private readonly List<CardInstance> _cards = new List<CardInstance>();
        private int _order;

        public int Cap { get; }
        public IReadOnlyList<CardInstance> Cards => _cards;
        public event Action<CardInstance> CardDiscarded;
        public int Count => _cards.Count;

        public Hand(int cap)
        {
            Cap = cap;
        }

        public void Add(CardInstance card, SpentPile spent)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            card.HandOrder = _order++;
            _cards.Add(card);

            while (Cap > 0 && _cards.Count > Cap)
            {
                var oldest = _cards[0];
                _cards.RemoveAt(0);
                spent?.Add(oldest);
                CardDiscarded?.Invoke(oldest);
            }
        }

        public bool Remove(CardInstance card)
        {
            return _cards.Remove(card);
        }

        public CardInstance Find(int id)
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Id == id)
                {
                    return _cards[i];
                }
            }

            return null;
        }

        public void Clear()
        {
            _cards.Clear();
        }
    }
}
