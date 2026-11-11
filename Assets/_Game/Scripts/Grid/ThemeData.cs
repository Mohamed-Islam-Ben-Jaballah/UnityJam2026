using System;
using DiceDrive.Entities;
using UnityEngine;

namespace DiceDrive.Grid
{
    [Serializable]
    public struct TerrainVisual
    {
        public TileType type;
        public Sprite sprite;
        public Color color;
    }

    [Serializable]
    public struct EnemyVisual
    {
        public EnemyData data;
        public Sprite sprite;
    }

    [Serializable]
    public struct TreasureVisual
    {
        public TreasureData data;
        public Sprite sprite;
    }

    [CreateAssetMenu(menuName = "Dice-Drive/Theme", fileName = "Theme")]
    public sealed class ThemeData : ScriptableObject
    {
        public TerrainVisual[] terrain;
        public Sprite playerMarker;
        public Sprite exitMarker;
        public EnemyVisual[] enemies;
        public TreasureVisual[] treasures;

        public Sprite GetTerrainSprite(TileType type)
        {
            if (terrain == null)
            {
                return null;
            }

            foreach (var visual in terrain)
            {
                if (visual.type == type)
                {
                    return visual.sprite;
                }
            }

            return null;
        }

        public Color GetTerrainColor(TileType type, Color fallback)
        {
            if (terrain == null)
            {
                return fallback;
            }

            foreach (var visual in terrain)
            {
                if (visual.type == type)
                {
                    return visual.color.a > 0f ? visual.color : fallback;
                }
            }

            return fallback;
        }

        public Sprite GetEnemySprite(EnemyData data)
        {
            if (enemies == null || data == null)
            {
                return null;
            }

            foreach (var visual in enemies)
            {
                if (visual.data == data)
                {
                    return visual.sprite;
                }
            }

            return null;
        }

        public Sprite GetTreasureSprite(TreasureData data)
        {
            if (treasures == null || data == null)
            {
                return null;
            }

            foreach (var visual in treasures)
            {
                if (visual.data == data)
                {
                    return visual.sprite;
                }
            }

            return null;
        }
    }
}
