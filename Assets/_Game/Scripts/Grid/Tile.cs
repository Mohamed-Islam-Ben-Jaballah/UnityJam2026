using System.Collections.Generic;

namespace DiceDrive.Grid
{
    public sealed class Tile
    {
        public TileType BaseType { get; private set; }
        public bool HasSand { get; private set; }
        public List<int> EnemyIds { get; } = new List<int>();
        public bool HasPlayer { get; set; }

        public Tile(TileType baseType)
        {
            BaseType = baseType;
        }

        public TileType EffectiveType
        {
            get
            {
                if (!HasSand)
                {
                    return BaseType;
                }

                if (BaseType == TileType.Hole || BaseType == TileType.Damaging)
                {
                    return TileType.Sand;
                }

                return BaseType;
            }
        }

        public bool IsTerrainWalkable =>
            EffectiveType == TileType.Floor ||
            EffectiveType == TileType.Exit ||
            EffectiveType == TileType.Treasure ||
            EffectiveType == TileType.Sand ||
            EffectiveType == TileType.Damaging;

        public bool IsDamaging => EffectiveType == TileType.Damaging;

        public bool IsHole => EffectiveType == TileType.Hole;

        public bool IsWall => EffectiveType == TileType.Wall;

        public void SetSand(bool hasSand)
        {
            HasSand = hasSand;
        }

        public void SetBaseType(TileType baseType)
        {
            BaseType = baseType;
        }
    }
}
