using System;
using System.Collections.Generic;
using DiceDrive.Cards;
using UnityEngine;

namespace DiceDrive.Grid
{
    [Serializable]
    public struct WeightedCard
    {
        public CardData card;
        public int weight;
    }

    [CreateAssetMenu(menuName = "Dice-Drive/Treasure", fileName = "Treasure")]
    public sealed class TreasureData : ScriptableObject
    {
        public string shortCode;
        public Sprite sprite;
        public int gold;
        public int heal;
        public int cardCount;
        public WeightedCard[] cards;

        public List<CardData> Draw(System.Random random)
        {
            var result = new List<CardData>();
            if (random == null || cards == null || cards.Length == 0 || cardCount <= 0)
            {
                return result;
            }

            var totalWeight = 0;
            foreach (var entry in cards)
            {
                if (entry.card != null)
                {
                    totalWeight += entry.weight > 0 ? entry.weight : 1;
                }
            }

            if (totalWeight <= 0)
            {
                return result;
            }

            for (var i = 0; i < cardCount; i++)
            {
                var roll = random.Next(totalWeight);
                var cursor = 0;
                foreach (var entry in cards)
                {
                    if (entry.card == null)
                    {
                        continue;
                    }

                    cursor += entry.weight > 0 ? entry.weight : 1;
                    if (roll < cursor)
                    {
                        result.Add(entry.card);
                        break;
                    }
                }
            }

            return result;
        }
    }
}
