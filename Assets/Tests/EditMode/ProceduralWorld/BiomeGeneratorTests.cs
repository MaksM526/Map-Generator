using MapGenerator.ProceduralWorld;
using NUnit.Framework;

public sealed class BiomeGeneratorTests
{
    private static readonly BiomeClassificationSettings Settings = new BiomeClassificationSettings(0.25f, 0.42f, 0.74f, 0.35f, 0.28f, 0.35f, 0.68f, 0.32f, 0.62f, 0.34f);

    [TestCase(0.1f, 0.8f, 0.1f, 0.5f, 0.5f, WorldBiome.Ocean)]
    [TestCase(0.8f, 0.3f, 0.1f, 0.5f, 0.5f, WorldBiome.Beach)]
    [TestCase(0.8f, 0.8f, 0.8f, 0.2f, 0.5f, WorldBiome.Snow)]
    [TestCase(0.8f, 0.8f, 0.8f, 0.6f, 0.5f, WorldBiome.Mountain)]
    [TestCase(0.8f, 0.8f, 0.1f, 0.2f, 0.2f, WorldBiome.Tundra)]
    [TestCase(0.8f, 0.8f, 0.1f, 0.2f, 0.5f, WorldBiome.Taiga)]
    [TestCase(0.8f, 0.8f, 0.1f, 0.8f, 0.2f, WorldBiome.Desert)]
    [TestCase(0.8f, 0.8f, 0.1f, 0.8f, 0.8f, WorldBiome.Jungle)]
    [TestCase(0.8f, 0.8f, 0.1f, 0.8f, 0.5f, WorldBiome.Savanna)]
    [TestCase(0.8f, 0.8f, 0.1f, 0.5f, 0.2f, WorldBiome.Plains)]
    [TestCase(0.8f, 0.8f, 0.1f, 0.5f, 0.5f, WorldBiome.Forest)]
    public void ClassifyBiome_ReturnsExpectedBiome(float continent, float height, float mountain, float temperature, float moisture, WorldBiome expected)
    {
        WorldBiome biome = BiomeGenerator.ClassifyBiome(continent, height, mountain, temperature, moisture, Settings);

        Assert.That(biome, Is.EqualTo(expected));
    }
}
