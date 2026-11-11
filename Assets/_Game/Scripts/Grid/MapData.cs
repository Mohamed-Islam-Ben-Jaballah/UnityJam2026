using System;
using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.Entities;
using UnityEngine;

namespace DiceDrive.Grid
{
    public enum MapKind
    {
        Grid,
        Linear
    }

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
        public TreasureData data;
        public int gold;
        public CardData[] cards;
    }

    [CreateAssetMenu(menuName = "Dice-Drive/Map", fileName = "Map")]
    public sealed class MapData : ScriptableObject
    {
        public int schemaVersion = 3;
        public string mapName;
        public MapKind mapKind;
        public int width = 8;
        public int height = 8;
        public TileType[] tiles;
        public bool[] pathTiles;
        public GridCoord[] branchTiles;
        public bool hasPlayerSpawn = true;
        public GridCoord playerSpawn;
        public GridCoord exitPos;
        public ExitSpawn[] exits;
        public EnemySpawn[] enemySpawns;
        public TreasureSpawn[] treasureSpawns;
        public Card3Tuple[] rewardPool;
        public ThemeData theme;

        public bool IsLinear => mapKind == MapKind.Linear;

        public bool IsPath(GridCoord coord)
        {
            if (!IsLinear || pathTiles == null || coord.X < 0 || coord.X >= width || coord.Y < 0 || coord.Y >= height)
            {
                return false;
            }

            var index = coord.Y * width + coord.X;
            return index < pathTiles.Length && pathTiles[index];
        }

        public bool IsBranchAt(GridCoord coord)
        {
            if (branchTiles == null)
            {
                return false;
            }

            foreach (var branch in branchTiles)
            {
                if (branch == coord)
                {
                    return true;
                }
            }

            return false;
        }

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

        public bool TryGetExitAt(GridCoord coord, out ExitSpawn spawn)
        {
            if (exits != null)
            {
                for (var i = 0; i < exits.Length; i++)
                {
                    if (exits[i].coord == coord)
                    {
                        spawn = exits[i];
                        return true;
                    }
                }
            }

            spawn = default;
            return false;
        }

        public bool TryGetTreasureAt(GridCoord coord, out TreasureSpawn spawn)
        {
            if (treasureSpawns != null)
            {
                for (var i = 0; i < treasureSpawns.Length; i++)
                {
                    if (treasureSpawns[i].coord == coord)
                    {
                        spawn = treasureSpawns[i];
                        return true;
                    }
                }
            }

            spawn = default;
            return false;
        }

        public bool IsFloor(GridCoord coord)
        {
            if (tiles == null || coord.X < 0 || coord.X >= width || coord.Y < 0 || coord.Y >= height)
            {
                return false;
            }

            return tiles[coord.Y * width + coord.X] == TileType.Floor;
        }

        public bool HasMarkerAt(GridCoord coord)
        {
            if (hasPlayerSpawn && playerSpawn == coord)
            {
                return true;
            }

            if (IsBranchAt(coord))
            {
                return true;
            }

            if (exits != null)
            {
                foreach (var exit in exits)
                {
                    if (exit.coord == coord)
                    {
                        return true;
                    }
                }
            }

            if (enemySpawns != null)
            {
                foreach (var spawn in enemySpawns)
                {
                    if (spawn.coord == coord)
                    {
                        return true;
                    }
                }
            }

            if (treasureSpawns != null)
            {
                foreach (var spawn in treasureSpawns)
                {
                    if (spawn.coord == coord)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void RemoveMarkersAt(GridCoord coord)
        {
            if (hasPlayerSpawn && playerSpawn == coord)
            {
                hasPlayerSpawn = false;
            }

            branchTiles = RemoveBranchesAt(branchTiles, coord);
            exits = RemoveExitsAt(exits, coord);
            enemySpawns = RemoveEnemiesAt(enemySpawns, coord);
            treasureSpawns = RemoveTreasuresAt(treasureSpawns, coord);
        }

        private static GridCoord[] RemoveBranchesAt(GridCoord[] source, GridCoord coord)
        {
            if (source == null)
            {
                return null;
            }

            var list = new List<GridCoord>(source.Length);
            foreach (var branch in source)
            {
                if (branch != coord)
                {
                    list.Add(branch);
                }
            }

            return list.ToArray();
        }

        private static ExitSpawn[] RemoveExitsAt(ExitSpawn[] source, GridCoord coord)
        {
            if (source == null)
            {
                return null;
            }

            var list = new System.Collections.Generic.List<ExitSpawn>(source.Length);
            foreach (var exit in source)
            {
                if (exit.coord != coord)
                {
                    list.Add(exit);
                }
            }

            return list.ToArray();
        }

        private static EnemySpawn[] RemoveEnemiesAt(EnemySpawn[] source, GridCoord coord)
        {
            if (source == null)
            {
                return null;
            }

            var list = new System.Collections.Generic.List<EnemySpawn>(source.Length);
            foreach (var spawn in source)
            {
                if (spawn.coord != coord)
                {
                    list.Add(spawn);
                }
            }

            return list.ToArray();
        }

        private static TreasureSpawn[] RemoveTreasuresAt(TreasureSpawn[] source, GridCoord coord)
        {
            if (source == null)
            {
                return null;
            }

            var list = new System.Collections.Generic.List<TreasureSpawn>(source.Length);
            foreach (var spawn in source)
            {
                if (spawn.coord != coord)
                {
                    list.Add(spawn);
                }
            }

            return list.ToArray();
        }
    }

    public sealed class PathGraph
    {
        private readonly MapData _map;
        private readonly HashSet<GridCoord> _path = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _terminalExits = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _reachable = new HashSet<GridCoord>();
        private readonly Dictionary<GridCoord, List<GridCoord>> _neighbors = new Dictionary<GridCoord, List<GridCoord>>();
        private readonly Dictionary<GridCoord, List<GridCoord>> _forward = new Dictionary<GridCoord, List<GridCoord>>();
        private readonly Dictionary<GridCoord, List<GridCoord>> _incoming = new Dictionary<GridCoord, List<GridCoord>>();
        private readonly Dictionary<GridCoord, int> _distance = new Dictionary<GridCoord, int>();
        private bool _hasDirectedCycle;

        public PathGraph(MapData map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            if (!map.IsLinear)
            {
                return;
            }

            for (var y = 0; y < map.height; y++)
            {
                for (var x = 0; x < map.width; x++)
                {
                    var coord = new GridCoord(x, y);
                    if (map.IsPath(coord))
                    {
                        _path.Add(coord);
                    }
                }
            }

            foreach (var coord in _path)
            {
                var neighbors = new List<GridCoord>(4);
                foreach (var direction in GridCoord.FourDirections)
                {
                    var next = coord + direction;
                    if (_path.Contains(next))
                    {
                        neighbors.Add(next);
                    }
                }

                _neighbors[coord] = neighbors;
                _forward[coord] = new List<GridCoord>(2);
                _incoming[coord] = new List<GridCoord>(2);
                if (neighbors.Count == 1 && map.TryGetExitAt(coord, out _) &&
                    (!map.hasPlayerSpawn || coord != map.playerSpawn))
                {
                    _terminalExits.Add(coord);
                }
            }

            var distanceQueue = new Queue<GridCoord>();
            foreach (var exit in _terminalExits)
            {
                _distance[exit] = 0;
                distanceQueue.Enqueue(exit);
            }

            while (distanceQueue.Count > 0)
            {
                var current = distanceQueue.Dequeue();
                var nextDistance = _distance[current] + 1;
                foreach (var next in _neighbors[current])
                {
                    if (_distance.ContainsKey(next))
                    {
                        continue;
                    }

                    _distance[next] = nextDistance;
                    distanceQueue.Enqueue(next);
                }
            }

            if (!map.hasPlayerSpawn || !_path.Contains(map.playerSpawn))
            {
                return;
            }

            var queue = new Queue<GridCoord>();
            var parent = new Dictionary<GridCoord, GridCoord>();
            _reachable.Add(map.playerSpawn);
            queue.Enqueue(map.playerSpawn);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var candidates = new List<GridCoord>(4);
                foreach (var next in _neighbors[current])
                {
                    if (parent.TryGetValue(current, out var previous) && next == previous)
                    {
                        continue;
                    }

                    candidates.Add(next);
                }

                if (candidates.Count == 0)
                {
                    continue;
                }

                List<GridCoord> forward;
                if (map.IsBranchAt(current) && candidates.Count >= 2)
                {
                    forward = candidates;
                }
                else if (candidates.Count == 1)
                {
                    forward = candidates;
                }
                else
                {
                    var lower = new List<GridCoord>(candidates.Count);
                    var currentDistance = _distance.TryGetValue(current, out var distance) ? distance : -1;
                    if (currentDistance >= 0)
                    {
                        foreach (var candidate in candidates)
                        {
                            if (_distance.TryGetValue(candidate, out var candidateDistance) &&
                                candidateDistance >= 0 && candidateDistance < currentDistance)
                            {
                                lower.Add(candidate);
                            }
                        }
                    }

                    forward = lower.Count > 0 ? lower : candidates;
                }

                foreach (var next in forward)
                {
                    AddForward(current, next);
                    if (_reachable.Add(next))
                    {
                        parent[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            foreach (var coord in _path)
            {
                if (_forward[coord].Count == 0 && map.TryGetExitAt(coord, out _) &&
                    (!map.hasPlayerSpawn || coord != map.playerSpawn))
                {
                    _terminalExits.Add(coord);
                }
            }

            _hasDirectedCycle = DetectCycle();
        }

        private void AddForward(GridCoord from, GridCoord to)
        {
            if (!_forward.TryGetValue(from, out var forward))
            {
                return;
            }

            if (!forward.Contains(to))
            {
                forward.Add(to);
            }

            if (_incoming.TryGetValue(to, out var incoming) && !incoming.Contains(from))
            {
                incoming.Add(from);
            }
        }

        private bool DetectCycle()
        {
            var state = new Dictionary<GridCoord, int>();
            foreach (var coord in _path)
            {
                if (DetectCycle(coord, state))
                {
                    return true;
                }
            }

            return false;
        }

        private bool DetectCycle(GridCoord coord, Dictionary<GridCoord, int> state)
        {
            state.TryGetValue(coord, out var currentState);
            if (currentState == 1)
            {
                return true;
            }

            if (currentState == 2)
            {
                return false;
            }

            state[coord] = 1;
            foreach (var next in _forward[coord])
            {
                if (DetectCycle(next, state))
                {
                    return true;
                }
            }

            state[coord] = 2;
            return false;
        }

        public MapData Map => _map;
        public IEnumerable<GridCoord> PathTiles => _path;
        public IEnumerable<GridCoord> TerminalExits => _terminalExits;

        public bool IsPath(GridCoord coord)
        {
            return _path.Contains(coord);
        }

        public IReadOnlyList<GridCoord> GetNeighbors(GridCoord coord)
        {
            return _neighbors.TryGetValue(coord, out var neighbors)
                ? neighbors
                : Array.Empty<GridCoord>();
        }

        public bool IsReachable(GridCoord coord)
        {
            return _reachable.Contains(coord);
        }

        public bool IsTerminalExit(GridCoord coord)
        {
            return _terminalExits.Contains(coord);
        }

        public IReadOnlyList<GridCoord> GetForwardOptions(GridCoord coord)
        {
            return _forward.TryGetValue(coord, out var forward)
                ? forward
                : Array.Empty<GridCoord>();
        }

        public IReadOnlyList<GridCoord> GetIncomingOptions(GridCoord coord)
        {
            return _incoming.TryGetValue(coord, out var incoming)
                ? incoming
                : Array.Empty<GridCoord>();
        }

        public bool IsBranch(GridCoord coord)
        {
            return GetForwardOptions(coord).Count > 1;
        }

        public bool IsMerge(GridCoord coord)
        {
            return GetIncomingOptions(coord).Count > 1;
        }

        public bool HasDirectedCycle => _hasDirectedCycle;

        public bool TryGetSingleForward(GridCoord coord, out GridCoord next)
        {
            var options = GetForwardOptions(coord);
            if (options.Count == 1)
            {
                next = options[0];
                return true;
            }

            next = default;
            return false;
        }

        public bool TryGetBounds(out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;

            foreach (var coord in _path)
            {
                minX = Math.Min(minX, coord.X);
                maxX = Math.Max(maxX, coord.X);
                minY = Math.Min(minY, coord.Y);
                maxY = Math.Max(maxY, coord.Y);
            }

            return minX != int.MaxValue;
        }
    }
}
