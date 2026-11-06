using System;
using DiceDrive.Cards;
using DiceDrive.Entities;
using UnityEngine;

namespace DiceDrive.Grid
{
    [Serializable]
    public struct EnemySpawn
    {
        public EnemyData data;
        public GridCoord coord;
    }

    [Serializable]
    public struct TreasureSpawn
    {
        public GridCoord coord;
        public int gold;
        public CardData[] cards;
    }

    [CreateAssetMenu(menuName = "Dice-Drive/Map", fileName = "Map")]
    public sealed class MapData : ScriptableObject
    {
        public int width = 8;
        public int height = 8;
        public TileType[] tiles;
        public GridCoord playerSpawn;
        public GridCoord exitPos;
        public EnemySpawn[] enemySpawns;
        public TreasureSpawn[] treasureSpawns;

        public GridMap CreateGrid()
        {
            var grid = new GridMap(width, height);
            if (tiles == null || tiles.Length == 0)
            {
                return grid;
            }

            var count = Math.Min(tiles.Length, width * height);
            for (var i = 0; i < count; i++)
            {
                var coord = new GridCoord(i % width, i / width);
                grid.SetBaseType(coord, tiles[i]);
            }

            return grid;
        }
    }
}
