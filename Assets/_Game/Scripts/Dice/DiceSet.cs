using System;
using System.Collections.Generic;

namespace DiceDrive.DiceSystem
{
    public sealed class DiceSet
    {
        private readonly List<Die> _dice;
        private int _index;

        public DiceSet(IEnumerable<Die> dice)
        {
            _dice = new List<Die>(dice ?? throw new ArgumentNullException(nameof(dice)));
        }

        public int Count => _dice.Count;

        public int Remaining => _dice.Count - _index;

        public Die Current => _index < _dice.Count ? _dice[_index] : null;

        public void BeginTurn()
        {
            _index = 0;
        }

        public bool Advance()
        {
            if (_index < _dice.Count)
            {
                _index++;
            }

            return _index < _dice.Count;
        }

        public void Add(Die die)
        {
            if (die == null)
            {
                throw new ArgumentNullException(nameof(die));
            }

            _dice.Add(die);
        }

        public int RollCurrent(Random random)
        {
            return Current != null ? Current.Roll(random) : 0;
        }
    }
}
