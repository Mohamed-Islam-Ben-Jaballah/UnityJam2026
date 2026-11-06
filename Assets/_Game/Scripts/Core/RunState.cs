using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.DiceSystem;

namespace DiceDrive.Core
{
    public sealed class RunState
    {
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Gold { get; set; }
        public List<CardInstance> RunDeck { get; } = new List<CardInstance>();
        public List<CardInstance> ClaimedTreasure { get; } = new List<CardInstance>();
        public List<Die> Dice { get; } = new List<Die>();
    }
}
