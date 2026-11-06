using DiceDrive.Grid;

namespace DiceDrive.Entities
{
    public sealed class PlayerState
    {
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public GridCoord Position { get; set; }

        public bool IsAlive => Hp > 0;
    }
}
