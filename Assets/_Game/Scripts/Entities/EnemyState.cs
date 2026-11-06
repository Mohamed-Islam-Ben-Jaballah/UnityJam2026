using DiceDrive.Grid;

namespace DiceDrive.Entities
{
    public sealed class EnemyState
    {
        public int Id { get; }
        public EnemyData Data { get; }
        public int Hp { get; set; }
        public GridCoord Position { get; set; }
        public int TelegraphTurn { get; set; }

        public bool IsAlive => Hp > 0;

        public EnemyState(int id, EnemyData data, GridCoord position)
        {
            Id = id;
            Data = data;
            Position = position;
            Hp = data != null ? data.maxHp : 1;
            TelegraphTurn = -1;
        }
    }
}
