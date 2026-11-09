using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.Grid;

namespace DiceDrive.Turn
{
    public sealed class LevelSnapshot
    {
        public int Hp;
        public int MaxHp;
        public int Gold;
        public List<CardInstance> Deck = new List<CardInstance>();
        public List<CardInstance> Hand = new List<CardInstance>();
        public List<CardInstance> ClaimedTreasure = new List<CardInstance>();
        public List<GridCoord> OpenedTreasure = new List<GridCoord>();
        public List<int> DiceSides = new List<int>();
    }
}
