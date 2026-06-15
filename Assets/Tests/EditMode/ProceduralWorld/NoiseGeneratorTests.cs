using MapGenerator.ProceduralWorld;
using NUnit.Framework;

public sealed class NoiseGeneratorTests
{
    [Test]
    public void Noise_ReturnsValueInMinusOneToOneRange()
    {
        for (int seed = -3; seed <= 3; seed++)
        {
            for (int i = 0; i < 64; i++)
            {
                float value = SimplexNoise.Noise(i * 0.173f - 3.1f, i * -0.097f + 2.4f, seed);
                Assert.That(value, Is.InRange(-1f, 1f));
            }
        }
    }

    [Test]
    public void Fractal_IsDeterministicForSameParameters()
    {
        float first = SimplexNoise.Fractal(0.123f, 9.876f, 5, 0.5f, 2f, 1337);
        float second = SimplexNoise.Fractal(0.123f, 9.876f, 5, 0.5f, 2f, 1337);

        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void RidgedFractal_ReturnsValueInZeroToOneRange()
    {
        for (int seed = -3; seed <= 3; seed++)
        {
            for (int i = 0; i < 64; i++)
            {
                float value = SimplexNoise.RidgedFractal(i * 0.173f - 3.1f, i * -0.097f + 2.4f, 5, seed);
                Assert.That(value, Is.InRange(0f, 1f));
            }
        }
    }
}
