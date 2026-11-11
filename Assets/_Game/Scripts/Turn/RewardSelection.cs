using System;
using System.Collections.Generic;
using DiceDrive.Grid;

namespace DiceDrive.Turn
{
    public sealed class RewardSelection
    {
        private readonly Card3Tuple[] _options = new Card3Tuple[3];
        private readonly bool[] _rerolled = new bool[3];
        private readonly HashSet<string> _shown = new HashSet<string>();
        private readonly List<Card3Tuple> _pool = new List<Card3Tuple>();
        private readonly Random _random;

        public RewardSelection(Card3Tuple[] pool, Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            if (pool != null)
            {
                foreach (var tuple in pool)
                {
                    if (tuple.a != null && tuple.b != null && tuple.c != null)
                    {
                        _pool.Add(tuple);
                    }
                }
            }

            for (var i = 0; i < _options.Length; i++)
            {
                _options[i] = DrawUnseen();
                if (_options[i].a == null)
                {
                    break;
                }
            }
        }

        public int Count => _options.Length;

        public Card3Tuple GetOption(int index)
        {
            return index >= 0 && index < _options.Length ? _options[index] : default;
        }

        public bool CanReroll(int index)
        {
            if (index < 0 || index >= _options.Length || _rerolled[index])
            {
                return false;
            }

            return HasUnseen();
        }

        public bool Reroll(int index)
        {
            if (!CanReroll(index))
            {
                return false;
            }

            _rerolled[index] = true;
            _options[index] = DrawUnseen();
            return _options[index].a != null;
        }

        public bool TryChoose(int index, out Card3Tuple tuple)
        {
            tuple = default;
            if (index < 0 || index >= _options.Length || _options[index].a == null)
            {
                return false;
            }

            tuple = _options[index];
            return true;
        }

        private Card3Tuple DrawUnseen()
        {
            if (_pool.Count == 0)
            {
                return default;
            }

            var start = _random.Next(_pool.Count);
            for (var offset = 0; offset < _pool.Count; offset++)
            {
                var candidate = _pool[(start + offset) % _pool.Count];
                var key = Card3TupleIdentity.Key(candidate);
                if (!_shown.Contains(key))
                {
                    _shown.Add(key);
                    return candidate;
                }
            }

            return default;
        }

        private bool HasUnseen()
        {
            foreach (var tuple in _pool)
            {
                if (!_shown.Contains(Card3TupleIdentity.Key(tuple)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
