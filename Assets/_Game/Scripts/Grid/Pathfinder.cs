using System;
using System.Collections.Generic;

namespace DiceDrive.Grid
{
    public enum PathState
    {
        Safe,
        Damaging,
        Unreachable
    }

    public sealed class PathResult
    {
        public PathState State { get; }
        public IReadOnlyList<GridCoord> Tiles { get; }
        public int HpCost { get; }
        public int Steps => Tiles.Count - 1;

        public PathResult(PathState state, IReadOnlyList<GridCoord> tiles, int hpCost)
        {
            State = state;
            Tiles = tiles;
            HpCost = hpCost;
        }
    }

    public sealed class Pathfinder
    {
        public IReadOnlyDictionary<GridCoord, PathResult> FindPaths(
            GridMap map,
            GridCoord start,
            int maxSteps,
            Func<GridCoord, bool> blocksWalk = null,
            Func<GridCoord, int> tileCost = null,
            int minSteps = 1)
        {
            var results = new Dictionary<GridCoord, PathResult>();
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (maxSteps < 0)
            {
                return results;
            }

            var bestCost = new Dictionary<GridCoord, int> { [start] = 0 };
            var bestSteps = new Dictionary<GridCoord, int> { [start] = 0 };
            var previous = new Dictionary<GridCoord, GridCoord>();
            var frontier = new List<GridCoord> { start };

            while (frontier.Count > 0)
            {
                var currentIndex = 0;
                for (var i = 1; i < frontier.Count; i++)
                {
                    var candidate = frontier[i];
                    var best = frontier[currentIndex];
                    if (bestCost[candidate] < bestCost[best] ||
                        (bestCost[candidate] == bestCost[best] && bestSteps[candidate] < bestSteps[best]))
                    {
                        currentIndex = i;
                    }
                }

                var current = frontier[currentIndex];
                frontier.RemoveAt(currentIndex);

                foreach (var direction in GridCoord.EightDirections)
                {
                    var next = current + direction;
                    if (!map.InBounds(next) || !map.IsTerrainWalkable(next))
                    {
                        continue;
                    }

                    if (blocksWalk != null && blocksWalk(next))
                    {
                        continue;
                    }

                    var steps = bestSteps[current] + 1;
                    if (steps > maxSteps)
                    {
                        continue;
                    }

                    var cost = bestCost[current] + Math.Max(0, tileCost?.Invoke(next) ?? 0);
                    var hasBetter = bestCost.TryGetValue(next, out var existingCost) &&
                                    (existingCost < cost ||
                                     (existingCost == cost && bestSteps[next] <= steps));

                    if (hasBetter)
                    {
                        continue;
                    }

                    bestCost[next] = cost;
                    bestSteps[next] = steps;
                    previous[next] = current;

                    if (!frontier.Contains(next))
                    {
                        frontier.Add(next);
                    }
                }
            }

            foreach (var pair in bestCost)
            {
                if (pair.Key == start || bestSteps[pair.Key] < minSteps)
                {
                    continue;
                }

                var path = BuildPath(previous, start, pair.Key);
                var state = pair.Value == 0 ? PathState.Safe : PathState.Damaging;
                results[pair.Key] = new PathResult(state, path, pair.Value);
            }

            return results;
        }

        private static List<GridCoord> BuildPath(
            IReadOnlyDictionary<GridCoord, GridCoord> previous,
            GridCoord start,
            GridCoord end)
        {
            var path = new List<GridCoord> { end };
            var current = end;

            while (current != start)
            {
                current = previous[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}
