using System.Collections.Generic;
using UnityEngine;

namespace MapGenerator.ProceduralWorld
{
    public readonly struct RiverTraceSettings
    {
        public readonly int MaxRiverLength;
        public readonly int MinRiverLength;
        public readonly int MinRiverTurns;
        public readonly int MaxRiverIntersections;
        public readonly float OceanContinentThreshold;

        public RiverTraceSettings(
            int maxRiverLength,
            int minRiverLength,
            int minRiverTurns,
            int maxRiverIntersections,
            float oceanContinentThreshold)
        {
            MaxRiverLength = maxRiverLength;
            MinRiverLength = minRiverLength;
            MinRiverTurns = minRiverTurns;
            MaxRiverIntersections = maxRiverIntersections;
            OceanContinentThreshold = oceanContinentThreshold;
        }
    }

    public static class RiverGenerator
    {
        public static bool TryTraceRiver(
            Vector2Int source,
            float[,] continents,
            float[,] height,
            bool[,] rivers,
            RiverTraceSettings settings,
            out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            Vector2Int current = source;
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            bool reachedOcean = false;
            bool reachedRiver = false;
            int intersections = 0;

            for (int step = 0; step < settings.MaxRiverLength; step++)
            {
                if (!IsInside(current, height))
                {
                    break;
                }

                if (continents[current.x, current.y] < settings.OceanContinentThreshold)
                {
                    reachedOcean = true;
                    break;
                }

                if (!visited.Add(current))
                {
                    break;
                }

                path.Add(current);

                if (rivers[current.x, current.y])
                {
                    intersections++;
                    reachedRiver = true;
                    break;
                }

                Vector2Int next = current;
                float nextHeight = height[current.x, current.y];
                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (ox == 0 && oy == 0) continue;
                        Vector2Int candidate = new Vector2Int(current.x + ox, current.y + oy);
                        if (!IsInside(candidate, height)) continue;
                        float candidateHeight = height[candidate.x, candidate.y] - (continents[candidate.x, candidate.y] < settings.OceanContinentThreshold ? 0.2f : 0f);
                        if (candidateHeight < nextHeight)
                        {
                            nextHeight = candidateHeight;
                            next = candidate;
                        }
                    }
                }

                if (next == current)
                {
                    break;
                }

                current = next;
            }

            return path.Count >= settings.MinRiverLength
                && CountRiverTurns(path) >= settings.MinRiverTurns
                && intersections <= settings.MaxRiverIntersections
                && (reachedOcean || reachedRiver);
        }

        public static int CountRiverTurns(IReadOnlyList<Vector2Int> path)
        {
            int turns = 0;
            Vector2Int previousDirection = Vector2Int.zero;
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int direction = path[i] - path[i - 1];
                if (previousDirection != Vector2Int.zero && direction != previousDirection)
                {
                    turns++;
                }

                previousDirection = direction;
            }

            return turns;
        }

        private static bool IsInside(Vector2Int p, float[,] map) => p.x >= 0 && p.y >= 0 && p.x < map.GetLength(0) && p.y < map.GetLength(1);
    }
}
