using System;

namespace DiceDrive.Grid
{
    public sealed class GridMap
    {
        public int Width { get; }
        public int Height { get; }

        private readonly Tile[] _tiles;

        public GridMap(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            _tiles = new Tile[width * height];

            for (var i = 0; i < _tiles.Length; i++)
            {
                _tiles[i] = new Tile(TileType.Floor);
            }
        }

        public bool InBounds(GridCoord coord)
        {
            return coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Height;
        }

        public Tile Get(GridCoord coord)
        {
            if (!InBounds(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord));
            }

            return _tiles[coord.Y * Width + coord.X];
        }

        public Tile Get(int x, int y)
        {
            return Get(new GridCoord(x, y));
        }

        public bool IsTerrainWalkable(GridCoord coord)
        {
            return InBounds(coord) && Get(coord).IsTerrainWalkable;
        }

        public bool IsDamaging(GridCoord coord)
        {
            return InBounds(coord) && Get(coord).IsDamaging;
        }

        public bool IsHole(GridCoord coord)
        {
            return InBounds(coord) && Get(coord).IsHole;
        }

        public bool IsWall(GridCoord coord)
        {
            return InBounds(coord) && Get(coord).IsWall;
        }

        public void SetBaseType(GridCoord coord, TileType type)
        {
            Get(coord).SetBaseType(type);
        }

        public void SetSand(GridCoord coord, bool value)
        {
            Get(coord).SetSand(value);
        }
    }
}
