using System;
using System.Collections.Generic;
using DiceDrive.Cards;
using UnityEngine;

namespace DiceDrive.Grid
{
    [Serializable]
    public struct Card3Tuple
    {
        public CardData a;
        public CardData b;
        public CardData c;

        public CardData[] ToArray()
        {
            return new[] { a, b, c };
        }

        public string Label()
        {
            return $"{NameOf(a)} / {NameOf(b)} / {NameOf(c)}";
        }

        private static string NameOf(CardData card)
        {
            if (card == null)
            {
                return "-";
            }

            return string.IsNullOrEmpty(card.cardName) ? card.name : card.cardName;
        }
    }

    public static class Card3TupleIdentity
    {
        public static string Key(Card3Tuple tuple)
        {
            var ids = new List<string>(3);
            Add(ids, tuple.a);
            Add(ids, tuple.b);
            Add(ids, tuple.c);
            ids.Sort();
            return string.Join(",", ids);
        }

        public static bool Equivalent(Card3Tuple left, Card3Tuple right)
        {
            return Key(left) == Key(right);
        }

        private static void Add(List<string> ids, CardData card)
        {
            if (card != null)
            {
                ids.Add(card.GetEntityId().ToString());
            }
        }
    }
}
