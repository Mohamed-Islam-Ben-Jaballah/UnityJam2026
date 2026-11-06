using System;
using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.Entities;
using DiceDrive.Grid;
using UnityEditor;
using UnityEngine;

namespace DiceDrive.EditorTools
{
    [CustomEditor(typeof(MapData))]
    public sealed class MapDataEditor : Editor
    {
        private enum Tool
        {
            Paint,
            PlayerSpawn,
            Exit,
            Enemy,
            Treasure
        }

        private Tool _tool;
        private TileType _brush = TileType.Floor;
        private EnemyData _enemyBrush;
        private int _treasureGold = 5;
        private CardData _treasureCard;

        public override void OnInspectorGUI()
        {
            var map = (MapData)target;
            serializedObject.Update();
            EnsureTiles(map);
            serializedObject.Update();

            EditorGUILayout.LabelField("Grid Painter", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var width = EditorGUILayout.IntField("Width", map.width);
            var height = EditorGUILayout.IntField("Height", map.height);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(map, "Resize Map");
                Resize(map, Mathf.Max(1, width), Mathf.Max(1, height));
                EditorUtility.SetDirty(map);
            }

            _tool = (Tool)EditorGUILayout.EnumPopup("Tool", _tool);
            if (_tool == Tool.Paint)
            {
                _brush = (TileType)EditorGUILayout.EnumPopup("Brush", _brush);
            }
            else if (_tool == Tool.Enemy)
            {
                _enemyBrush = (EnemyData)EditorGUILayout.ObjectField("Enemy", _enemyBrush, typeof(EnemyData), false);
            }
            else if (_tool == Tool.Treasure)
            {
                _treasureGold = EditorGUILayout.IntField("Gold", _treasureGold);
                _treasureCard = (CardData)EditorGUILayout.ObjectField("Card", _treasureCard, typeof(CardData), false);
            }

            DrawGrid(map);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawns", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSpawn"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("exitPos"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enemySpawns"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("treasureSpawns"), true);

            EditorGUILayout.Space();
            if (GUILayout.Button("Fill With Floor"))
            {
                Undo.RecordObject(map, "Fill With Floor");
                for (var i = 0; i < map.tiles.Length; i++)
                {
                    map.tiles[i] = TileType.Floor;
                }

                EditorUtility.SetDirty(map);
            }

            if (GUILayout.Button("Clear Enemy Spawns"))
            {
                Undo.RecordObject(map, "Clear Enemy Spawns");
                map.enemySpawns = Array.Empty<EnemySpawn>();
                EditorUtility.SetDirty(map);
            }

            if (GUILayout.Button("Clear Treasure Spawns"))
            {
                Undo.RecordObject(map, "Clear Treasure Spawns");
                map.treasureSpawns = Array.Empty<TreasureSpawn>();
                EditorUtility.SetDirty(map);
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGrid(MapData map)
        {
            for (var y = map.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (var x = 0; x < map.width; x++)
                {
                    var index = y * map.width + x;
                    var tile = index >= 0 && index < map.tiles.Length ? map.tiles[index] : TileType.Floor;
                    var label = Marker(map, x, y);
                    if (string.IsNullOrEmpty(label))
                    {
                        label = Short(tile);
                    }

                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = ColorFor(tile);
                    if (GUILayout.Button(label, GUILayout.Width(28f), GUILayout.Height(28f)))
                    {
                        Apply(map, x, y);
                    }

                    GUI.backgroundColor = old;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void Apply(MapData map, int x, int y)
        {
            var index = y * map.width + x;
            if (index < 0 || index >= map.tiles.Length)
            {
                return;
            }

            Undo.RecordObject(map, "Edit Map");

            switch (_tool)
            {
                case Tool.Paint:
                    map.tiles[index] = _brush;
                    break;
                case Tool.PlayerSpawn:
                    map.playerSpawn = new GridCoord(x, y);
                    break;
                case Tool.Exit:
                    map.exitPos = new GridCoord(x, y);
                    map.tiles[index] = TileType.Exit;
                    break;
                case Tool.Enemy:
                    if (_enemyBrush != null)
                    {
                        AddEnemy(map, x, y);
                    }

                    break;
                case Tool.Treasure:
                    AddTreasure(map, x, y);
                    map.tiles[index] = TileType.Treasure;
                    break;
            }

            EditorUtility.SetDirty(map);
            serializedObject.Update();
            Repaint();
        }

        private void AddEnemy(MapData map, int x, int y)
        {
            var list = map.enemySpawns != null ? new List<EnemySpawn>(map.enemySpawns) : new List<EnemySpawn>();
            list.Add(new EnemySpawn { data = _enemyBrush, coord = new GridCoord(x, y) });
            map.enemySpawns = list.ToArray();
        }

        private void AddTreasure(MapData map, int x, int y)
        {
            var list = map.treasureSpawns != null ? new List<TreasureSpawn>(map.treasureSpawns) : new List<TreasureSpawn>();
            var cards = _treasureCard != null ? new[] { _treasureCard } : Array.Empty<CardData>();
            list.Add(new TreasureSpawn { coord = new GridCoord(x, y), gold = _treasureGold, cards = cards });
            map.treasureSpawns = list.ToArray();
        }

        private static string Marker(MapData map, int x, int y)
        {
            if (map.playerSpawn.X == x && map.playerSpawn.Y == y)
            {
                return "P";
            }

            if (map.exitPos.X == x && map.exitPos.Y == y)
            {
                return "X";
            }

            if (map.enemySpawns != null)
            {
                foreach (var spawn in map.enemySpawns)
                {
                    if (spawn.coord.X == x && spawn.coord.Y == y)
                    {
                        return "E";
                    }
                }
            }

            if (map.treasureSpawns != null)
            {
                foreach (var spawn in map.treasureSpawns)
                {
                    if (spawn.coord.X == x && spawn.coord.Y == y)
                    {
                        return "T";
                    }
                }
            }

            return string.Empty;
        }

        private static string Short(TileType type)
        {
            switch (type)
            {
                case TileType.Exit: return "X";
                case TileType.Treasure: return "T";
                case TileType.Hole: return "O";
                case TileType.Sand: return "S";
                case TileType.Damaging: return "!";
                case TileType.Wall: return "#";
                default: return ".";
            }
        }

        private static Color ColorFor(TileType type)
        {
            switch (type)
            {
                case TileType.Exit: return new Color(0.5f, 0.9f, 0.5f);
                case TileType.Treasure: return new Color(0.9f, 0.8f, 0.3f);
                case TileType.Hole: return new Color(0.2f, 0.3f, 0.7f);
                case TileType.Sand: return new Color(0.8f, 0.75f, 0.5f);
                case TileType.Damaging: return new Color(0.9f, 0.5f, 0.2f);
                case TileType.Wall: return new Color(0.5f, 0.5f, 0.55f);
                default: return new Color(0.7f, 0.7f, 0.7f);
            }
        }

        private static void EnsureTiles(MapData map)
        {
            if (map.width <= 0)
            {
                map.width = 1;
            }

            if (map.height <= 0)
            {
                map.height = 1;
            }

            if (map.tiles == null || map.tiles.Length != map.width * map.height)
            {
                Undo.RecordObject(map, "Initialize Map");
                Resize(map, map.width, map.height);
                EditorUtility.SetDirty(map);
            }
        }

        private static void Resize(MapData map, int width, int height)
        {
            var oldTiles = map.tiles;
            var oldWidth = map.width;
            var oldHeight = map.height;

            var tiles = new TileType[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    if (oldTiles != null && x < oldWidth && y < oldHeight && oldTiles.Length > y * oldWidth + x)
                    {
                        tiles[index] = oldTiles[y * oldWidth + x];
                    }
                    else
                    {
                        tiles[index] = TileType.Floor;
                    }
                }
            }

            map.width = width;
            map.height = height;
            map.tiles = tiles;
        }
    }
}
