using System;
using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.Entities;
using DiceDrive.Grid;
using UnityEditor;
using UnityEngine;

namespace DiceDrive.EditorTools
{
    public sealed class MapBuilderWindow : EditorWindow
    {
        private enum PaintMode
        {
            Terrain,
            Path,
            Branch,
            Player,
            Exit,
            Enemy,
            Treasure,
            Erase
        }

        private static readonly string[] TerrainNames = { "Floor", "Hole", "Damaging", "Wall" };
        private static readonly TileType[] TerrainValues =
        {
            TileType.Floor,
            TileType.Hole,
            TileType.Damaging,
            TileType.Wall
        };

        private const float CellSize = 28f;

        private MapData _map;
        private Vector2 _scroll;
        private PaintMode _mode = PaintMode.Terrain;
        private int _terrainIndex;
        private EnemyData _enemyBrush;
        private TreasureData _treasureBrush;
        private MapData _exitDestination;
        private ThemeData _exitTheme;
        private bool _painting;
        private GridCoord _lastPainted = new GridCoord(-1, -1);
        private GUIStyle _cellStyle;

        [MenuItem("Dice-Drive/Map Builder")]
        public static void OpenWindow()
        {
            GetWindow<MapBuilderWindow>("Map Builder");
        }

        public static void Open(MapData map)
        {
            var window = GetWindow<MapBuilderWindow>("Map Builder");
            window._map = map;
            window.Repaint();
        }

        private void OnEnable()
        {
            if (Selection.activeObject is MapData map)
            {
                _map = map;
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is MapData map)
            {
                _map = map;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (_cellStyle == null)
            {
                _cellStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 9
                };
            }

            DrawToolbar();
            if (_map == null)
            {
                EditorGUILayout.HelpBox("Select a MapData asset or create a new map.", MessageType.Info);
                return;
            }

            EnsureTiles(_map);
            DrawMapHeader();
            DrawPalette();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(430f));
            DrawGrid();
            EditorGUILayout.EndScrollView();

            DrawExitList();
            DrawRewardPool();
            DrawAlerts();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            _map = (MapData)EditorGUILayout.ObjectField("Map", _map, typeof(MapData), false);
            if (GUILayout.Button("New Map", GUILayout.Width(90f)))
            {
                CreateNewMap(MapKind.Grid);
            }

            if (GUILayout.Button("New Linear", GUILayout.Width(90f)))
            {
                CreateNewMap(MapKind.Linear);
            }

            using (new EditorGUI.DisabledScope(_map == null))
            {
                if (GUILayout.Button("Save", GUILayout.Width(70f)))
                {
                    Save();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMapHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            _map.mapName = EditorGUILayout.TextField("Name", _map.mapName);
            if (GUILayout.Button("Rename", GUILayout.Width(80f)))
            {
                RenameAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            var kind = (MapKind)EditorGUILayout.EnumPopup("Kind", _map.mapKind);
            if (EditorGUI.EndChangeCheck())
            {
                _map.mapKind = kind;
                EditorUtility.SetDirty(_map);
            }

            EditorGUI.BeginChangeCheck();
            var width = EditorGUILayout.IntSlider("Width", _map.width, 4, 64);
            var height = EditorGUILayout.IntSlider("Height", _map.height, 4, 64);
            if (EditorGUI.EndChangeCheck())
            {
                Resize(width, height);
            }

            EditorGUILayout.LabelField("Tiles", $"{_map.width} x {_map.height} = {_map.width * _map.height}");
        }

        private void DrawPalette()
        {
            EditorGUILayout.Space();
            _mode = (PaintMode)EditorGUILayout.EnumPopup("Tool", _mode);

            switch (_mode)
            {
                case PaintMode.Terrain:
                    _terrainIndex = EditorGUILayout.Popup("Terrain", _terrainIndex, TerrainNames);
                    if (_map.IsLinear && TerrainValues[_terrainIndex] == TileType.Wall)
                    {
                        EditorGUILayout.HelpBox("Linear maps do not allow walls. Use Hole or Damaging instead.", MessageType.Warning);
                    }
                    break;
                case PaintMode.Path:
                    EditorGUILayout.HelpBox("Paint path tiles. Linear maps use only painted path tiles.", MessageType.None);
                    break;
                case PaintMode.Branch:
                    EditorGUILayout.HelpBox("Paint a branch marker on a path tile with two or three forward paths.", MessageType.None);
                    break;
                case PaintMode.Enemy:
                    _enemyBrush = (EnemyData)EditorGUILayout.ObjectField("Enemy", _enemyBrush, typeof(EnemyData), false);
                    break;
                case PaintMode.Treasure:
                    _treasureBrush = (TreasureData)EditorGUILayout.ObjectField("Treasure", _treasureBrush, typeof(TreasureData), false);
                    break;
                case PaintMode.Exit:
                    _exitDestination = (MapData)EditorGUILayout.ObjectField("Next Map", _exitDestination, typeof(MapData), false);
                    _exitTheme = (ThemeData)EditorGUILayout.ObjectField("Theme", _exitTheme, typeof(ThemeData), false);
                    break;
            }

            EditorGUILayout.HelpBox(
                _painting
                    ? "Drag on the grid to paint. Release to end the stroke."
                    : "Click or drag on the grid to paint the selected tool.",
                MessageType.None);
        }

        private void DrawGrid()
        {
            var area = GUILayoutUtility.GetRect(
                _map.width * CellSize,
                _map.height * CellSize,
                GUILayout.ExpandWidth(false),
                GUILayout.ExpandHeight(false));

            for (var y = _map.height - 1; y >= 0; y--)
            {
                for (var x = 0; x < _map.width; x++)
                {
                    var coord = new GridCoord(x, y);
                    var rect = new Rect(
                        area.x + x * CellSize,
                        area.y + (_map.height - 1 - y) * CellSize,
                        CellSize,
                        CellSize);

                    EditorGUI.DrawRect(rect, CellColor(coord));
                    GUI.Label(rect, CellLabel(coord), _cellStyle);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(0f, 0f, 0f, 0.35f));
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), new Color(0f, 0f, 0f, 0.35f));
                }
            }

            HandleGridEvents(area);
        }

        private void HandleGridEvents(Rect area)
        {
            var current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && area.Contains(current.mousePosition))
            {
                _painting = true;
                _lastPainted = new GridCoord(-1, -1);
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint Map");
                ApplyFromMouse(area, current.mousePosition);
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseDrag && _painting && area.Contains(current.mousePosition))
            {
                ApplyFromMouse(area, current.mousePosition);
                current.Use();
                Repaint();
                return;
            }

            if (current.type == EventType.MouseUp && _painting)
            {
                _painting = false;
                current.Use();
            }
        }

        private void ApplyFromMouse(Rect area, Vector2 mouse)
        {
            var x = Mathf.FloorToInt((mouse.x - area.x) / CellSize);
            var y = _map.height - 1 - Mathf.FloorToInt((mouse.y - area.y) / CellSize);
            var coord = new GridCoord(x, y);
            if (coord.X < 0 || coord.X >= _map.width || coord.Y < 0 || coord.Y >= _map.height)
            {
                return;
            }

            if (coord == _lastPainted)
            {
                return;
            }

            _lastPainted = coord;
            ApplyAt(coord);
        }

        private void ApplyAt(GridCoord coord)
        {
            Undo.RecordObject(_map, "Paint Map");
            switch (_mode)
            {
                case PaintMode.Terrain:
                    if (_map.IsLinear && !_map.IsPath(coord))
                    {
                        Debug.LogWarning($"Path tile required before painting terrain at {coord}.");
                        return;
                    }

                    var terrain = TerrainValues[_terrainIndex];
                    if (_map.IsLinear && terrain == TileType.Wall)
                    {
                        Debug.LogWarning("Linear maps cannot contain walls.");
                        return;
                    }

                    if (terrain != TileType.Floor && _map.HasMarkerAt(coord))
                    {
                        Debug.LogWarning($"Cannot paint {terrain} under a marker at {coord}. Erase the marker first.");
                        return;
                    }

                    SetTile(coord, terrain);
                    break;

                case PaintMode.Path:
                    if (!_map.IsLinear)
                    {
                        Debug.LogWarning("Path tiles are only available on linear maps.");
                        return;
                    }

                    SetPath(coord, true);
                    if (!_map.HasMarkerAt(coord))
                    {
                        SetTile(coord, TileType.Floor);
                    }
                    break;

                case PaintMode.Branch:
                    if (!_map.IsLinear)
                    {
                        Debug.LogWarning("Branch markers are only available on linear maps.");
                        return;
                    }

                    if (!_map.IsPath(coord))
                    {
                        Debug.LogWarning($"Paint a path tile before adding a branch at {coord}.");
                        return;
                    }

                    _map.RemoveMarkersAt(coord);
                    AddBranch(coord);
                    SetTile(coord, TileType.Floor);
                    break;

                case PaintMode.Player:
                    if (_map.IsLinear && !_map.IsPath(coord))
                    {
                        Debug.LogWarning($"Paint a path tile before placing the start at {coord}.");
                        return;
                    }

                    _map.RemoveMarkersAt(coord);
                    _map.hasPlayerSpawn = true;
                    _map.playerSpawn = coord;
                    SetTile(coord, TileType.Floor);
                    break;

                case PaintMode.Exit:
                    if (_map.IsLinear && !_map.IsPath(coord))
                    {
                        Debug.LogWarning($"Paint a path tile before placing an exit at {coord}.");
                        return;
                    }

                    _map.RemoveMarkersAt(coord);
                    AddExit(coord);
                    SetTile(coord, TileType.Floor);
                    break;

                case PaintMode.Enemy:
                    if (_enemyBrush == null)
                    {
                        Debug.LogWarning("Select an EnemyData asset before painting enemy spawns.");
                        return;
                    }

                    if (_map.IsLinear && !_map.IsPath(coord))
                    {
                        Debug.LogWarning($"Paint a path tile before placing an enemy at {coord}.");
                        return;
                    }

                    _map.RemoveMarkersAt(coord);
                    AddEnemy(coord);
                    SetTile(coord, TileType.Floor);
                    break;

                case PaintMode.Treasure:
                    if (_treasureBrush == null)
                    {
                        Debug.LogWarning("Select a TreasureData asset before painting treasures.");
                        return;
                    }

                    if (_map.IsLinear && !_map.IsPath(coord))
                    {
                        Debug.LogWarning($"Paint a path tile before placing a treasure at {coord}.");
                        return;
                    }

                    _map.RemoveMarkersAt(coord);
                    AddTreasure(coord);
                    SetTile(coord, TileType.Floor);
                    break;

                case PaintMode.Erase:
                    _map.RemoveMarkersAt(coord);
                    SetTile(coord, TileType.Floor);
                    if (_map.IsLinear)
                    {
                        SetPath(coord, false);
                    }
                    break;
            }

            EditorUtility.SetDirty(_map);
        }

        private void AddExit(GridCoord coord)
        {
            var exits = _map.exits != null ? new List<ExitSpawn>(_map.exits) : new List<ExitSpawn>();
            exits.Add(new ExitSpawn
            {
                coord = coord,
                destination = new MapDestination
                {
                    kind = DestinationKind.Map,
                    map = _exitDestination,
                    theme = _exitTheme
                }
            });
            _map.exits = exits.ToArray();
        }

        private void AddBranch(GridCoord coord)
        {
            if (_map.IsBranchAt(coord))
            {
                return;
            }

            var branches = _map.branchTiles != null
                ? new List<GridCoord>(_map.branchTiles)
                : new List<GridCoord>();
            branches.Add(coord);
            _map.branchTiles = branches.ToArray();
        }

        private void AddEnemy(GridCoord coord)
        {
            var enemies = _map.enemySpawns != null
                ? new List<EnemySpawn>(_map.enemySpawns)
                : new List<EnemySpawn>();
            enemies.Add(new EnemySpawn { data = _enemyBrush, coord = coord });
            _map.enemySpawns = enemies.ToArray();
        }

        private void AddTreasure(GridCoord coord)
        {
            var treasures = _map.treasureSpawns != null
                ? new List<TreasureSpawn>(_map.treasureSpawns)
                : new List<TreasureSpawn>();
            treasures.Add(new TreasureSpawn { coord = coord, data = _treasureBrush });
            _map.treasureSpawns = treasures.ToArray();
        }

        private void SetTile(GridCoord coord, TileType type)
        {
            if (coord.X < 0 || coord.X >= _map.width || coord.Y < 0 || coord.Y >= _map.height)
            {
                return;
            }

            _map.tiles[coord.Y * _map.width + coord.X] = type;
        }

        private void SetPath(GridCoord coord, bool value)
        {
            if (coord.X < 0 || coord.X >= _map.width || coord.Y < 0 || coord.Y >= _map.height)
            {
                return;
            }

            if (_map.pathTiles == null || _map.pathTiles.Length != _map.width * _map.height)
            {
                _map.pathTiles = new bool[_map.width * _map.height];
            }

            _map.pathTiles[coord.Y * _map.width + coord.X] = value;
        }

        private void DrawExitList()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Exits", EditorStyles.boldLabel);

            if (_map.exits == null || _map.exits.Length == 0)
            {
                EditorGUILayout.HelpBox("No exits. Paint an exit with the Exit tool.", MessageType.Warning);
                return;
            }

            var removeIndex = -1;
            for (var i = 0; i < _map.exits.Length; i++)
            {
                var exit = _map.exits[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Exit {i + 1}", GUILayout.Width(60f));
                EditorGUILayout.LabelField(exit.coord.ToString(), GUILayout.Width(70f));
                exit.destination.kind = (DestinationKind)EditorGUILayout.EnumPopup(exit.destination.kind, GUILayout.Width(80f));
                if (exit.destination.kind == DestinationKind.Map)
                {
                    exit.destination.map = (MapData)EditorGUILayout.ObjectField(exit.destination.map, typeof(MapData), false);
                    exit.destination.theme = (ThemeData)EditorGUILayout.ObjectField(exit.destination.theme, typeof(ThemeData), false);
                }
                else
                {
                    exit.destination.prefab = (GameObject)EditorGUILayout.ObjectField(exit.destination.prefab, typeof(GameObject), false);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
                _map.exits[i] = exit;
            }

            if (removeIndex >= 0)
            {
                var list = new List<ExitSpawn>(_map.exits);
                list.RemoveAt(removeIndex);
                _map.exits = list.ToArray();
                EditorUtility.SetDirty(_map);
            }
        }

        private void DrawRewardPool()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reward Pool", EditorStyles.boldLabel);

            if (_map.rewardPool == null)
            {
                _map.rewardPool = new Card3Tuple[0];
            }

            EditorGUILayout.LabelField("Tuples", _map.rewardPool.Length.ToString());
            if (_map.rewardPool.Length < 6)
            {
                EditorGUILayout.HelpBox("The reward pool should contain at least six tuples.", MessageType.Warning);
            }

            var removeIndex = -1;
            for (var i = 0; i < _map.rewardPool.Length; i++)
            {
                var tuple = _map.rewardPool[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(24f));
                tuple.a = (CardData)EditorGUILayout.ObjectField(tuple.a, typeof(CardData), false);
                tuple.b = (CardData)EditorGUILayout.ObjectField(tuple.b, typeof(CardData), false);
                tuple.c = (CardData)EditorGUILayout.ObjectField(tuple.c, typeof(CardData), false);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();
                _map.rewardPool[i] = tuple;
            }

            if (removeIndex >= 0)
            {
                var list = new List<Card3Tuple>(_map.rewardPool);
                list.RemoveAt(removeIndex);
                _map.rewardPool = list.ToArray();
                EditorUtility.SetDirty(_map);
            }

            if (GUILayout.Button("Add Reward Tuple"))
            {
                var list = new List<Card3Tuple>(_map.rewardPool);
                list.Add(default);
                _map.rewardPool = list.ToArray();
                EditorUtility.SetDirty(_map);
            }
        }

        private void DrawAlerts()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Soft Alerts", EditorStyles.boldLabel);

            if (_map.IsLinear)
            {
                var graph = new PathGraph(_map);
                var branches = 0;
                var merges = 0;
                var terminals = 0;
                var tiles = 0;
                foreach (var coord in graph.PathTiles)
                {
                    tiles++;
                    if (graph.IsBranch(coord))
                    {
                        branches++;
                    }

                    if (graph.IsMerge(coord))
                    {
                        merges++;
                    }

                    if (graph.IsTerminalExit(coord))
                    {
                        terminals++;
                    }
                }

                EditorGUILayout.HelpBox(
                    $"Path graph: {tiles} tiles, {branches} inferred branches, {merges} inferred merges, {terminals} terminal exits.",
                    MessageType.Info);
            }

            var issues = MapValidator.Validate(_map);
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No soft alerts.", MessageType.Info);
                return;
            }

            foreach (var issue in issues)
            {
                var type = issue.severity == MapIssueSeverity.Error
                    ? MessageType.Error
                    : issue.severity == MapIssueSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;
                var suffix = issue.coord.HasValue ? $" at {issue.coord.Value}" : string.Empty;
                EditorGUILayout.HelpBox($"{issue.code}: {issue.message}{suffix}", type);
            }
        }

        private string CellLabel(GridCoord coord)
        {
            if (_map.IsLinear && !_map.IsPath(coord))
            {
                return string.Empty;
            }

            if (_map.IsBranchAt(coord))
            {
                return "B";
            }

            if (_map.hasPlayerSpawn && _map.playerSpawn == coord)
            {
                return "P";
            }

            if (_map.exits != null)
            {
                for (var i = 0; i < _map.exits.Length; i++)
                {
                    if (_map.exits[i].coord == coord)
                    {
                        return (i + 1).ToString();
                    }
                }
            }

            if (_map.enemySpawns != null)
            {
                foreach (var spawn in _map.enemySpawns)
                {
                    if (spawn.coord == coord && spawn.data != null)
                    {
                        return string.IsNullOrEmpty(spawn.data.shortCode)
                            ? ShortName(spawn.data.enemyName)
                            : spawn.data.shortCode;
                    }
                }
            }

            if (_map.treasureSpawns != null)
            {
                foreach (var spawn in _map.treasureSpawns)
                {
                    if (spawn.coord == coord)
                    {
                        return spawn.data != null && !string.IsNullOrEmpty(spawn.data.shortCode)
                            ? spawn.data.shortCode
                            : "T";
                    }
                }
            }

            if (_map.IsLinear && _map.IsPath(coord))
            {
                return "\u00b7";
            }

            return TerrainShort(_map.tiles[coord.Y * _map.width + coord.X]);
        }

        private Color CellColor(GridCoord coord)
        {
            if (_map.IsLinear && !_map.IsPath(coord))
            {
                return new Color(0.12f, 0.12f, 0.14f);
            }

            if (_map.IsBranchAt(coord))
            {
                return new Color(0.95f, 0.55f, 0.1f);
            }

            if (_map.hasPlayerSpawn && _map.playerSpawn == coord)
            {
                return new Color(0.1f, 0.7f, 0.9f);
            }

            if (_map.exits != null)
            {
                foreach (var exit in _map.exits)
                {
                    if (exit.coord == coord)
                    {
                        return new Color(0.2f, 0.8f, 0.3f);
                    }
                }
            }

            if (_map.enemySpawns != null)
            {
                foreach (var spawn in _map.enemySpawns)
                {
                    if (spawn.coord == coord)
                    {
                        return new Color(0.8f, 0.25f, 0.2f);
                    }
                }
            }

            if (_map.treasureSpawns != null)
            {
                foreach (var spawn in _map.treasureSpawns)
                {
                    if (spawn.coord == coord)
                    {
                        return new Color(0.8f, 0.65f, 0.15f);
                    }
                }
            }

            var type = _map.tiles[coord.Y * _map.width + coord.X];
            switch (type)
            {
                case TileType.Hole:
                    return new Color(0.15f, 0.25f, 0.55f);
                case TileType.Damaging:
                    return new Color(0.75f, 0.35f, 0.15f);
                case TileType.Wall:
                    return new Color(0.35f, 0.35f, 0.4f);
                case TileType.Sand:
                    return new Color(0.7f, 0.65f, 0.45f);
                default:
                    return _map.IsLinear && _map.IsPath(coord)
                        ? new Color(0.32f, 0.42f, 0.36f)
                        : new Color(0.55f, 0.55f, 0.55f);
            }
        }

        private void CreateNewMap(MapKind kind)
        {
            EnsureFolder("Assets/_Game/Data/Maps");
            var fileName = kind == MapKind.Linear ? "NewLinearMap.asset" : "NewMap.asset";
            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/_Game/Data/Maps/{fileName}");
            var map = ScriptableObject.CreateInstance<MapData>();
            map.schemaVersion = 3;
            map.mapKind = kind;
            map.mapName = kind == MapKind.Linear ? "NewLinearMap" : "NewMap";
            map.width = 8;
            map.height = 8;
            map.hasPlayerSpawn = true;
            map.playerSpawn = new GridCoord(0, 0);
            map.tiles = new TileType[64];
            map.pathTiles = new bool[64];
            for (var i = 0; i < map.tiles.Length; i++)
            {
                map.tiles[i] = TileType.Floor;
            }

            if (kind == MapKind.Linear)
            {
                map.pathTiles[0] = true;
                map.pathTiles[1] = true;
            }

            map.branchTiles = new GridCoord[0];
            map.exits = new ExitSpawn[0];
            map.enemySpawns = new EnemySpawn[0];
            map.treasureSpawns = new TreasureSpawn[0];
            map.rewardPool = new Card3Tuple[0];

            AssetDatabase.CreateAsset(map, path);
            AssetDatabase.SaveAssets();
            _map = map;
            Selection.activeObject = map;
            EditorGUIUtility.PingObject(map);
        }

        private void RenameAsset()
        {
            var path = AssetDatabase.GetAssetPath(_map);
            var newName = string.IsNullOrWhiteSpace(_map.mapName) ? _map.name : _map.mapName.Trim();
            _map.mapName = newName;
            var error = AssetDatabase.RenameAsset(path, newName);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning(error);
            }
            else
            {
                AssetDatabase.SaveAssets();
            }
        }

        private void Resize(int width, int height)
        {
            Undo.RecordObject(_map, "Resize Map");
            var oldTiles = _map.tiles;
            var oldPath = _map.pathTiles;
            var oldWidth = _map.width;
            var oldHeight = _map.height;
            var tiles = new TileType[width * height];
            var path = new bool[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    if (x < oldWidth && y < oldHeight)
                    {
                        var oldIndex = y * oldWidth + x;
                        tiles[index] = oldTiles != null && oldTiles.Length > oldIndex
                            ? oldTiles[oldIndex]
                            : TileType.Floor;
                        path[index] = oldPath != null && oldPath.Length > oldIndex && oldPath[oldIndex];
                    }
                    else
                    {
                        tiles[index] = TileType.Floor;
                    }
                }
            }

            _map.width = width;
            _map.height = height;
            _map.tiles = tiles;
            _map.pathTiles = path;

            if (_map.hasPlayerSpawn && !InBounds(_map.playerSpawn))
            {
                _map.hasPlayerSpawn = false;
            }

            _map.branchTiles = FilterBranches(_map.branchTiles);
            _map.exits = FilterExits(_map.exits);
            _map.enemySpawns = FilterEnemies(_map.enemySpawns);
            _map.treasureSpawns = FilterTreasures(_map.treasureSpawns);

            EditorUtility.SetDirty(_map);
        }

        private GridCoord[] FilterBranches(GridCoord[] source)
        {
            if (source == null)
            {
                return null;
            }

            var list = new List<GridCoord>();
            foreach (var branch in source)
            {
                if (InBounds(branch))
                {
                    list.Add(branch);
                }
            }

            return list.ToArray();
        }

        private ExitSpawn[] FilterExits(ExitSpawn[] source)
        {
            if (source == null)
            {
                return null;
            }

            var list = new List<ExitSpawn>();
            foreach (var exit in source)
            {
                if (InBounds(exit.coord))
                {
                    list.Add(exit);
                }
            }

            return list.ToArray();
        }

        private EnemySpawn[] FilterEnemies(EnemySpawn[] source)
        {
            if (source == null)
            {
                return null;
            }

            var list = new List<EnemySpawn>();
            foreach (var spawn in source)
            {
                if (InBounds(spawn.coord))
                {
                    list.Add(spawn);
                }
            }

            return list.ToArray();
        }

        private TreasureSpawn[] FilterTreasures(TreasureSpawn[] source)
        {
            if (source == null)
            {
                return null;
            }

            var list = new List<TreasureSpawn>();
            foreach (var spawn in source)
            {
                if (InBounds(spawn.coord))
                {
                    list.Add(spawn);
                }
            }

            return list.ToArray();
        }

        private bool InBounds(GridCoord coord)
        {
            return coord.X >= 0 && coord.X < _map.width && coord.Y >= 0 && coord.Y < _map.height;
        }

        private static void EnsureTiles(MapData map)
        {
            if (map.width <= 0)
            {
                map.width = 8;
            }

            if (map.height <= 0)
            {
                map.height = 8;
            }

            if (map.schemaVersion < 3)
            {
                map.schemaVersion = 3;
            }

            if (map.tiles == null || map.tiles.Length != map.width * map.height)
            {
                var tiles = new TileType[map.width * map.height];
                for (var i = 0; i < tiles.Length; i++)
                {
                    tiles[i] = TileType.Floor;
                }

                map.tiles = tiles;
            }

            if (map.pathTiles == null || map.pathTiles.Length != map.width * map.height)
            {
                var pathTiles = new bool[map.width * map.height];
                if (map.pathTiles != null)
                {
                    Array.Copy(map.pathTiles, pathTiles, Math.Min(map.pathTiles.Length, pathTiles.Length));
                }

                map.pathTiles = pathTiles;
            }

            if (map.branchTiles == null)
            {
                map.branchTiles = new GridCoord[0];
            }

            if (map.exits == null)
            {
                map.exits = new ExitSpawn[0];
            }

            if (map.enemySpawns == null)
            {
                map.enemySpawns = new EnemySpawn[0];
            }

            if (map.treasureSpawns == null)
            {
                map.treasureSpawns = new TreasureSpawn[0];
            }

            if (map.rewardPool == null)
            {
                map.rewardPool = new Card3Tuple[0];
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private void Save()
        {
            EditorUtility.SetDirty(_map);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string TerrainShort(TileType type)
        {
            switch (type)
            {
                case TileType.Hole: return "O";
                case TileType.Damaging: return "!";
                case TileType.Wall: return "#";
                case TileType.Sand: return "S";
                default: return ".";
            }
        }

        private static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "E";
            }

            return name.Length <= 2 ? name : name.Substring(0, 2);
        }
    }
}
