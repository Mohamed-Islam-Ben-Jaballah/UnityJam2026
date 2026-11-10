using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiceDrive.Cards;
using DiceDrive.Core;
using DiceDrive.DiceSystem;
using DiceDrive.Entities;
using DiceDrive.Grid;
using DiceDrive.QA;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceDrive.EditorTools
{
    public static class QaSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameSessionQA.unity";
        private const string ConfigPath = "Assets/_Game/Data/RunConfig_QA.asset";
        private const string MapPath = "Assets/_Game/Data/Maps/QA_Level.asset";

        [MenuItem("Dice-Drive/QA/Build QA Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/_Game/Data/Maps");
            EnsureFolder("Assets/_Game/Data/Dice");

            var d6 = EnsureDice(6);
            var walk = LoadCard("Walk");
            var leapol = LoadCard("Leapol");
            var squimi = LoadCard("Squimi");
            var heilin = LoadCard("Heilin");

            var config = EnsureAsset<RunConfig>(ConfigPath);
            config.maxHp = 6;
            config.handSize = 5;
            config.handCap = 8;
            config.freeRerollsPerMap = 1;
            config.torchSeconds = 240f;
            config.startingDeck = BuildStarterDeck(walk, leapol, squimi, heilin);
            config.startingDice = new[] { d6 };
            EditorUtility.SetDirty(config);

            var map = EnsureAsset<MapData>(MapPath);
            BuildMap(map, walk, heilin);
            EditorUtility.SetDirty(map);

            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject("QA Bootstrap");
            SceneManager.MoveGameObjectToScene(go, scene);
            var bootstrap = go.AddComponent<QaBootstrap>();
            bootstrap.runConfig = config;
            bootstrap.mapData = map;
            bootstrap.seed = 12345;
            bootstrap.levelNumber = 1;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("QA scene built at " + ScenePath);
        }

        private static void BuildMap(MapData map, CardData walk, CardData heilin)
        {
            map.width = 8;
            map.height = 8;
            map.tiles = new TileType[map.width * map.height];
            for (var i = 0; i < map.tiles.Length; i++)
            {
                map.tiles[i] = TileType.Floor;
            }

            map.playerSpawn = new GridCoord(0, 0);
            map.exitPos = new GridCoord(7, 7);
            SetTile(map, 7, 7, TileType.Exit);

            SetTile(map, 2, 2, TileType.Hole);
            SetTile(map, 3, 2, TileType.Hole);
            SetTile(map, 2, 3, TileType.Hole);
            SetTile(map, 5, 1, TileType.Damaging);
            SetTile(map, 5, 2, TileType.Damaging);
            SetTile(map, 4, 4, TileType.Wall);
            SetTile(map, 4, 5, TileType.Wall);
            SetTile(map, 5, 4, TileType.Wall);
            SetTile(map, 1, 6, TileType.Treasure);
            SetTile(map, 6, 2, TileType.Treasure);

            map.enemySpawns = new[]
            {
                new EnemySpawn { data = LoadEnemy("Sentry"), coord = new GridCoord(3, 5) },
                new EnemySpawn { data = LoadEnemy("Archer"), coord = new GridCoord(6, 5) },
                new EnemySpawn { data = LoadEnemy("Stalker"), coord = new GridCoord(5, 6) },
                new EnemySpawn { data = LoadEnemy("Spitter"), coord = new GridCoord(2, 6) }
            };

            map.treasureSpawns = new[]
            {
                new TreasureSpawn { coord = new GridCoord(1, 6), gold = 5, cards = walk != null ? new[] { walk } : new CardData[0] },
                new TreasureSpawn { coord = new GridCoord(6, 2), gold = 5, cards = heilin != null ? new[] { heilin } : new CardData[0] }
            };
        }

        private static CardData[] BuildStarterDeck(CardData walk, CardData leapol, CardData squimi, CardData heilin)
        {
            var cards = new List<CardData>();
            Add(cards, walk, 8);
            Add(cards, leapol, 2);
            Add(cards, squimi, 2);
            Add(cards, heilin, 2);
            return cards.ToArray();
        }

        private static void Add(List<CardData> cards, CardData card, int count)
        {
            if (card == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                cards.Add(card);
            }
        }

        private static void SetTile(MapData map, int x, int y, TileType type)
        {
            var index = y * map.width + x;
            if (index >= 0 && index < map.tiles.Length)
            {
                map.tiles[index] = type;
            }
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static DiceData EnsureDice(int sides)
        {
            var path = "Assets/_Game/Data/Dice/d" + sides + ".asset";
            var die = AssetDatabase.LoadAssetAtPath<DiceData>(path);
            if (die == null)
            {
                die = ScriptableObject.CreateInstance<DiceData>();
                die.sides = sides;
                AssetDatabase.CreateAsset(die, path);
            }
            else
            {
                die.sides = sides;
                EditorUtility.SetDirty(die);
            }

            return die;
        }

        private static CardData LoadCard(string name)
        {
            var card = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Game/Data/Cards/" + name + ".asset");
            if (card == null)
            {
                Debug.LogWarning("Missing card asset: " + name);
            }

            return card;
        }

        private static EnemyData LoadEnemy(string name)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/Data/Enemies/" + name + ".asset");
            if (enemy == null)
            {
                Debug.LogWarning("Missing enemy asset: " + name);
            }

            return enemy;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
