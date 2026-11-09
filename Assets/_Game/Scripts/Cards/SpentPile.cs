using System.Collections.Generic;

namespace DiceDrive.Cards
{
    public sealed class SpentPile
    {
        private readonly List<CardInstance> _cards = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Cards => _cards;

        public int Count => _cards.Count;

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
