using DiceDrive.Cards;
using DiceDrive.DiceSystem;
using UnityEngine;

namespace DiceDrive.Core
{
    [CreateAssetMenu(menuName = "Dice-Drive/Run Config", fileName = "RunConfig")]
    public sealed class RunConfig : ScriptableObject
    {
        public CardData[] startingDeck;
        public DiceData[] startingDice;
        public int maxHp = 6;
        public int handSize = 5;
        public int handCap = 8;
        public float torchSeconds = 240f;
    }
}
