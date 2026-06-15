using MapGenerator.ProceduralWorld;
using NUnit.Framework;
using UnityEngine;

public sealed class RiverGeneratorTests
{
    [Test]
    public void TryTraceRiver_FollowsDescendingPathToOcean()
    {
        float[,] continents = CreateFilledMap(5, 5, 1f);
        float[,] height = CreateFilledMap(5, 5, 1f);
        bool[,] rivers = new bool[5, 5];
        for (int x = 0; x < 5; x++)
        {
            height[x, 2] = 1f - x * 0.2f;
        }

        continents[4, 2] = 0f;
        RiverTraceSettings settings = new RiverTraceSettings(16, 3, 0, 0, 0.25f);

        bool traced = RiverGenerator.TryTraceRiver(new Vector2Int(0, 2), continents, height, rivers, settings, out var path);

        Assert.That(traced, Is.True);
        Assert.That(path, Is.EqualTo(new[] { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2) }));
    }

    [Test]
    public void TryTraceRiver_RejectsTooShortPath()
    {
        float[,] continents = CreateFilledMap(3, 3, 1f);
        float[,] height = CreateFilledMap(3, 3, 1f);
        bool[,] rivers = new bool[3, 3];
        height[0, 1] = 0.8f;
        height[1, 1] = 0.6f;
        continents[2, 1] = 0f;
        RiverTraceSettings settings = new RiverTraceSettings(8, 3, 0, 0, 0.25f);

        bool traced = RiverGenerator.TryTraceRiver(new Vector2Int(0, 1), continents, height, rivers, settings, out _);

        Assert.That(traced, Is.False);
    }

    [Test]
    public void CountRiverTurns_CountsDirectionChanges()
    {
        var path = new[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(2, 0),
            new Vector2Int(2, 1),
            new Vector2Int(3, 1)
        };

        Assert.That(RiverGenerator.CountRiverTurns(path), Is.EqualTo(2));
    }

    private static float[,] CreateFilledMap(int width, int height, float value)
    {
        float[,] map = new float[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                map[x, y] = value;
            }
        }

        return map;
    }
}
