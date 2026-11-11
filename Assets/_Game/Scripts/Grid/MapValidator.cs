using System;
using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.Grid;
using UnityEngine;

namespace DiceDrive.Grid
{
    public enum MapIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    public struct MapIssue
    {
        public MapIssueSeverity severity;
        public string code;
        public string message;
        public GridCoord? coord;

        public MapIssue(MapIssueSeverity severity, string code, string message, GridCoord? coord = null)
        {
            this.severity = severity;
            this.code = code;
            this.message = message;
            this.coord = coord;
        }
    }

    public static class MapValidator
    {
        public static List<MapIssue> Validate(MapData map)
        {
            var issues = new List<MapIssue>();
            if (map == null)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Error, "map.null", "Map data is missing."));
                return issues;
            }

            if (map.width < 1 || map.height < 1)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Error, "size.zero", "Map width and height must be at least 1."));
            }
            else if (map.width < 4 || map.width > 64 || map.height < 4 || map.height > 64)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Warning, "size.range", "Map size is outside the 4 to 64 editor range."));
            }

            if (map.tiles == null || map.tiles.Length != map.width * map.height)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Error, "tiles.length", "The tile array does not match width times height."));
            }

            if (!map.hasPlayerSpawn)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Warning, "start.missing", "The map has no player start."));
            }
            else if (!InBounds(map, map.playerSpawn))
            {
                issues.Add(new MapIssue(MapIssueSeverity.Error, "start.bounds", "The player start is outside the map.", map.playerSpawn));
            }

            if (map.exits == null || map.exits.Length == 0)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Warning, "exits.none", "The map has no exits."));
            }
            else
            {
                for (var i = 0; i < map.exits.Length; i++)
                {
                    var exit = map.exits[i];
                    if (!InBounds(map, exit.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Error, "exit.bounds", $"Exit {i + 1} is outside the map.", exit.coord));
                    }
                    else if (!map.IsFloor(exit.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "exit.floor", $"Exit {i + 1} is not on a Floor cell.", exit.coord));
                    }

                    if (map.IsLinear && !map.IsPath(exit.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "exit.path", $"Exit {i + 1} is not on a path tile.", exit.coord));
                    }

                    if (!exit.destination.IsValid)
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "exit.destination", $"Exit {i + 1} has no valid destination.", exit.coord));
                    }
                }
            }

            ValidateMarkers(map, issues);
            ValidateTreasure(map, issues);
            ValidateRewards(map, issues);
            if (map.IsLinear)
            {
                ValidateLinear(map, issues);
            }

            return issues;
        }

        private static void ValidateMarkers(MapData map, List<MapIssue> issues)
        {
            if (map.enemySpawns != null)
            {
                foreach (var spawn in map.enemySpawns)
                {
                    if (spawn.data == null)
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "enemy.null", "An enemy spawn has no EnemyData.", spawn.coord));
                    }

                    if (!InBounds(map, spawn.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Error, "enemy.bounds", "An enemy spawn is outside the map.", spawn.coord));
                    }
                    else if (!map.IsFloor(spawn.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "enemy.floor", "An enemy spawn is not on a Floor cell.", spawn.coord));
                    }

                    if (map.IsLinear && !map.IsPath(spawn.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "enemy.path", "An enemy spawn is not on a path tile.", spawn.coord));
                    }
                }
            }

            if (map.treasureSpawns != null)
            {
                foreach (var spawn in map.treasureSpawns)
                {
                    if (spawn.data == null && (spawn.cards == null || spawn.cards.Length == 0))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "treasure.empty", "A treasure has no TreasureData and no legacy cards.", spawn.coord));
                    }

                    if (!InBounds(map, spawn.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Error, "treasure.bounds", "A treasure spawn is outside the map.", spawn.coord));
                    }
                    else if (!map.IsFloor(spawn.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "treasure.floor", "A treasure spawn is not on a Floor cell.", spawn.coord));
                    }

                    if (map.IsLinear && !map.IsPath(spawn.coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "treasure.path", "A treasure spawn is not on a path tile.", spawn.coord));
                    }
                }
            }
        }

        private static void ValidateTreasure(MapData map, List<MapIssue> issues)
        {
            if (map.treasureSpawns == null)
            {
                return;
            }

            for (var i = 0; i < map.treasureSpawns.Length; i++)
            {
                for (var j = i + 1; j < map.treasureSpawns.Length; j++)
                {
                    if (map.treasureSpawns[i].coord == map.treasureSpawns[j].coord)
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "treasure.duplicate", "Two treasures share a cell.", map.treasureSpawns[i].coord));
                    }
                }
            }
        }

        private static void ValidateRewards(MapData map, List<MapIssue> issues)
        {
            if (map.rewardPool == null || map.rewardPool.Length < 6)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Warning, "reward.count", "The reward pool has fewer than six tuples."));
                return;
            }

            for (var i = 0; i < map.rewardPool.Length; i++)
            {
                var tuple = map.rewardPool[i];
                if (tuple.a == null || tuple.b == null || tuple.c == null)
                {
                    issues.Add(new MapIssue(MapIssueSeverity.Warning, "reward.null", $"Reward tuple {i + 1} has a null card."));
                }

                if (map.IsLinear)
                {
                    CheckLinearCard(map, tuple.a, $"Reward tuple {i + 1} card A", issues);
                    CheckLinearCard(map, tuple.b, $"Reward tuple {i + 1} card B", issues);
                    CheckLinearCard(map, tuple.c, $"Reward tuple {i + 1} card C", issues);
                }

                for (var j = i + 1; j < map.rewardPool.Length; j++)
                {
                    if (Card3TupleIdentity.Equivalent(tuple, map.rewardPool[j]))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "reward.duplicate", $"Reward tuples {i + 1} and {j + 1} are identical."));
                    }
                }
            }
        }

        private static void ValidateLinear(MapData map, List<MapIssue> issues)
        {
            if (map.pathTiles == null || map.pathTiles.Length != map.width * map.height)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Error, "path.length", "The path tile array does not match width times height."));
                return;
            }

            var graph = new PathGraph(map);
            var pathCount = 0;
            var start = map.hasPlayerSpawn ? map.playerSpawn : default;
            var startIsPath = map.hasPlayerSpawn && map.IsPath(start);
            var reachable = startIsPath ? CollectReachable(graph, start) : new HashSet<GridCoord>();

            for (var y = 0; y < map.height; y++)
            {
                for (var x = 0; x < map.width; x++)
                {
                    var coord = new GridCoord(x, y);
                    if (!map.IsPath(coord))
                    {
                        continue;
                    }

                    pathCount++;
                    var type = map.tiles != null && map.tiles.Length == map.width * map.height
                        ? map.tiles[coord.Y * map.width + coord.X]
                        : TileType.Floor;

                    if (type == TileType.Wall || type == TileType.Sand)
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.terrain", "A path tile cannot be Wall or Sand.", coord));
                    }

                    if (map.HasMarkerAt(coord) && type != TileType.Floor)
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.overlay", "A marker is stacked on a non-Floor path tile.", coord));
                    }

                    if (startIsPath && !reachable.Contains(coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.disconnected", "A path tile is not connected to the start.", coord));
                    }

                    var forwardCount = graph.GetForwardOptions(coord).Count;
                    if (!graph.IsReachable(coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.noexit", "A path tile is not reachable by the inferred forward flow. Check for a missing branch marker.", coord));
                    }
                    else if (forwardCount == 0 && !graph.IsTerminalExit(coord))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.noexit", "A path tile has no forward path and is not a terminal exit.", coord));
                    }

                    var markedBranch = map.IsBranchAt(coord);
                    if (forwardCount > 1 && !markedBranch)
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "branch.missing", "A junction with multiple forward paths needs a branch marker.", coord));
                    }

                    if (markedBranch)
                    {
                        if (type != TileType.Floor)
                        {
                            issues.Add(new MapIssue(MapIssueSeverity.Warning, "branch.overlay", "A branch marker must be on a Floor path tile.", coord));
                        }

                        if (forwardCount < 2)
                        {
                            issues.Add(new MapIssue(MapIssueSeverity.Warning, "branch.notbranch", "A branch marker needs at least two forward path options.", coord));
                        }

                        if (graph.GetIncomingOptions(coord).Count >= 2)
                        {
                            issues.Add(new MapIssue(MapIssueSeverity.Warning, "branch.merge", "A branch marker is on a merge tile. Keep split and merge on separate tiles.", coord));
                        }
                    }

                    var neighborCount = graph.GetNeighbors(coord).Count;
                    if (neighborCount == 1 && coord != start && !map.TryGetExitAt(coord, out _))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.deadend", "A path dead end must have an exit.", coord));
                    }
                }
            }

            if (pathCount == 0)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.empty", "The linear map has no path tiles."));
            }

            if (graph.HasDirectedCycle)
            {
                issues.Add(new MapIssue(MapIssueSeverity.Warning, "path.cycle", "The path graph contains a directed cycle."));
            }

            if (map.hasPlayerSpawn)
            {
                if (!startIsPath)
                {
                    issues.Add(new MapIssue(MapIssueSeverity.Warning, "start.path", "The player start is not on a path tile.", start));
                }
                else if (graph.GetNeighbors(start).Count != 1)
                {
                    issues.Add(new MapIssue(MapIssueSeverity.Warning, "start.endpoint", "The player start must be a path endpoint.", start));
                }

                if (map.TryGetExitAt(start, out _))
                {
                    issues.Add(new MapIssue(MapIssueSeverity.Warning, "start.exit", "The player start cannot also be an exit.", start));
                }
            }

            if (map.branchTiles != null)
            {
                foreach (var branch in map.branchTiles)
                {
                    if (!map.IsPath(branch))
                    {
                        issues.Add(new MapIssue(MapIssueSeverity.Warning, "branch.path", "A branch marker is not on a path tile.", branch));
                    }
                }
            }

            if (map.treasureSpawns != null)
            {
                foreach (var spawn in map.treasureSpawns)
                {
                    if (spawn.data == null)
                    {
                        continue;
                    }

                    if (spawn.data.cards != null)
                    {
                        foreach (var weighted in spawn.data.cards)
                        {
                            CheckLinearCard(map, weighted.card, "Treasure card", issues, spawn.coord);
                        }
                    }

                    if (spawn.cards != null)
                    {
                        foreach (var card in spawn.cards)
                        {
                            CheckLinearCard(map, card, "Legacy treasure card", issues, spawn.coord);
                        }
                    }
                }
            }
        }

        private static HashSet<GridCoord> CollectReachable(PathGraph graph, GridCoord start)
        {
            var reachable = new HashSet<GridCoord>();
            var queue = new Queue<GridCoord>();
            reachable.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in graph.GetNeighbors(current))
                {
                    if (reachable.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return reachable;
        }

        private static void CheckLinearCard(
            MapData map,
            CardData card,
            string label,
            List<MapIssue> issues,
            GridCoord? coord = null)
        {
            if (!map.IsLinear || card == null)
            {
                return;
            }

            if (!IsLinearAllowed(card))
            {
                issues.Add(new MapIssue(
                    MapIssueSeverity.Warning,
                    "linear.card",
                    $"{label} is not allowed on a linear map.",
                    coord));
            }
        }

        public static bool IsLinearAllowed(CardData card)
        {
            if (card == null)
            {
                return true;
            }

            switch (card.effect)
            {
                case CardEffectType.Walk:
                case CardEffectType.Jump:
                case CardEffectType.Damage:
                case CardEffectType.Heal:
                case CardEffectType.Draw:
                    return true;
                default:
                    return false;
            }
        }

        private static bool InBounds(MapData map, GridCoord coord)
        {
            return coord.X >= 0 && coord.X < map.width && coord.Y >= 0 && coord.Y < map.height;
        }
    }
}
