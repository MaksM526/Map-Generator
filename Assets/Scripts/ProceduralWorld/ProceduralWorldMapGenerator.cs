using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapGenerator.ProceduralWorld
{
    public enum WorldMapPreview
    {
        Biomes,
        Height,
        Continents,
        Mountains,
        Hills,
        Temperature,
        Moisture,
        Rivers
    }

    public enum WorldBiome
    {
        Ocean,
        Beach,
        Tundra,
        Taiga,
        Plains,
        Forest,
        Savanna,
        Desert,
        Jungle,
        Mountain,
        Snow
    }

    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public sealed class ProceduralWorldMapGenerator : MonoBehaviour
    {
        [Header("Preview")]
        [SerializeField] private WorldMapPreview preview = WorldMapPreview.Biomes;
        [SerializeField, Min(16)] private int mapSize = 256;
        [SerializeField] private bool autoRegenerate = true;
        [SerializeField] private int seed = 1337;

        [Header("Continents")]
        [SerializeField, Min(0.001f)] private float voronoiScale = 5f;
        [SerializeField, Min(0.001f)] private float continentSimplexScale = 2.2f;
        [SerializeField, Range(0f, 1f)] private float coastThreshold = 0.48f;
        [SerializeField, Range(0f, 1f)] private float coastBlend = 0.16f;

        [Header("Relief")]
        [SerializeField, Min(0.001f)] private float mountainScale = 7.5f;
        [SerializeField, Range(1, 8)] private int mountainOctaves = 5;
        [SerializeField, Min(0.001f)] private float hillScale = 18f;
        [SerializeField, Range(1, 8)] private int hillOctaves = 4;

        [Header("Climate")]
        [SerializeField, Min(0.001f)] private float temperatureNoiseScale = 3.5f;
        [SerializeField, Range(0f, 1f)] private float temperatureNoiseStrength = 0.18f;
        [SerializeField, Min(0.001f)] private float moistureScale = 5f;
        [SerializeField, Range(0f, 1f)] private float coastalMoistureStrength = 0.35f;

        [Header("Rivers")]
        [SerializeField, Range(0, 256)] private int riverCount = 48;
        [SerializeField, Range(16, 2048)] private int maxRiverLength = 512;
        [SerializeField, Range(0.45f, 1f)] private float riverSourceMinHeight = 0.62f;

        [Header("Output")]
        [SerializeField] private Texture2D generatedTexture;
        [SerializeField] private Material targetMaterial;

        private float[,] continents;
        private float[,] mountains;
        private float[,] hills;
        private float[,] temperature;
        private float[,] moisture;
        private float[,] height;
        private bool[,] rivers;
        private WorldBiome[,] biomes;

        public Texture2D GeneratedTexture => generatedTexture;

        private void OnEnable()
        {
            if (autoRegenerate)
            {
                Generate();
            }
        }

        private void OnValidate()
        {
            mapSize = Mathf.Max(16, mapSize);
            if (autoRegenerate)
            {
                Generate();
            }
        }

        [ContextMenu("Generate World Map")]
        public void Generate()
        {
            AllocateMaps();
            GenerateContinents();
            GenerateRelief();
            GenerateTemperature();
            GenerateMoisture();
            GenerateRivers();
            GenerateBiomes();
            RenderPreviewTexture();
        }

        private void AllocateMaps()
        {
            continents = new float[mapSize, mapSize];
            mountains = new float[mapSize, mapSize];
            hills = new float[mapSize, mapSize];
            temperature = new float[mapSize, mapSize];
            moisture = new float[mapSize, mapSize];
            height = new float[mapSize, mapSize];
            rivers = new bool[mapSize, mapSize];
            biomes = new WorldBiome[mapSize, mapSize];
        }

        private void GenerateContinents()
        {
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float voronoi = VoronoiNoise(uv * voronoiScale, seed);
                    float simplex = SimplexNoise.Fractal(uv.x * continentSimplexScale, uv.y * continentSimplexScale, 4, 0.5f, 2f, seed + 17);
                    float combined = Mathf.Clamp01(voronoi + 0.35f * simplex);
                    continents[x, y] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(coastThreshold - coastBlend, coastThreshold + coastBlend, combined));
                }
            }
        }

        private void GenerateRelief()
        {
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float ridge = SimplexNoise.RidgedFractal(uv.x * mountainScale, uv.y * mountainScale, mountainOctaves, seed + 101);
                    float hill = Mathf.Clamp01(0.5f + 0.5f * SimplexNoise.Fractal(uv.x * hillScale, uv.y * hillScale, hillOctaves, 0.5f, 2f, seed + 211));
                    mountains[x, y] = ridge * continents[x, y];
                    hills[x, y] = hill * continents[x, y];
                    height[x, y] = Mathf.Clamp01(continents[x, y] * 0.7f + mountains[x, y] * 0.2f + hills[x, y] * 0.1f);
                }
            }
        }

        private void GenerateTemperature()
        {
            for (int y = 0; y < mapSize; y++)
            {
                float latitude = 1f - Mathf.Abs((y / (float)(mapSize - 1)) * 2f - 1f);
                for (int x = 0; x < mapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float noise = SimplexNoise.Noise(uv.x * temperatureNoiseScale, uv.y * temperatureNoiseScale, seed + 307) * temperatureNoiseStrength;
                    float altitudeCooling = mountains[x, y] * 0.28f;
                    temperature[x, y] = Mathf.Clamp01(latitude + noise - altitudeCooling);
                }
            }
        }

        private void GenerateMoisture()
        {
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float baseMoisture = Mathf.Clamp01(0.5f + 0.5f * SimplexNoise.Fractal(uv.x * moistureScale, uv.y * moistureScale, 4, 0.55f, 2f, seed + 409));
                    float waterInfluence = 1f - continents[x, y];
                    float coastInfluence = EstimateCoastalMoisture(x, y);
                    moisture[x, y] = Mathf.Clamp01(baseMoisture + waterInfluence + coastInfluence * coastalMoistureStrength);
                }
            }
        }

        private float EstimateCoastalMoisture(int x, int y)
        {
            const int radius = 5;
            float influence = 0f;
            for (int oy = -radius; oy <= radius; oy++)
            {
                for (int ox = -radius; ox <= radius; ox++)
                {
                    int sx = Mathf.Clamp(x + ox, 0, mapSize - 1);
                    int sy = Mathf.Clamp(y + oy, 0, mapSize - 1);
                    if (continents[sx, sy] < 0.2f)
                    {
                        float distance = Mathf.Max(1f, Mathf.Sqrt(ox * ox + oy * oy));
                        influence = Mathf.Max(influence, 1f - distance / radius);
                    }
                }
            }

            return influence;
        }

        private void GenerateRivers()
        {
            System.Random random = new System.Random(seed + 503);
            for (int i = 0; i < riverCount; i++)
            {
                Vector2Int source = FindRiverSource(random);
                TraceRiver(source);
            }
        }

        private Vector2Int FindRiverSource(System.Random random)
        {
            Vector2Int best = new Vector2Int(random.Next(mapSize), random.Next(mapSize));
            float bestScore = -1f;
            for (int attempt = 0; attempt < 128; attempt++)
            {
                int x = random.Next(mapSize);
                int y = random.Next(mapSize);
                float score = height[x, y] + mountains[x, y] * 0.5f;
                if (height[x, y] >= riverSourceMinHeight && score > bestScore)
                {
                    best = new Vector2Int(x, y);
                    bestScore = score;
                }
            }

            return best;
        }

        private void TraceRiver(Vector2Int source)
        {
            Vector2Int current = source;
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            for (int step = 0; step < maxRiverLength; step++)
            {
                if (!IsInside(current) || continents[current.x, current.y] < 0.25f || !visited.Add(current))
                {
                    break;
                }

                rivers[current.x, current.y] = true;
                moisture[current.x, current.y] = 1f;

                Vector2Int next = current;
                float nextHeight = height[current.x, current.y];
                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (ox == 0 && oy == 0) continue;
                        Vector2Int candidate = new Vector2Int(current.x + ox, current.y + oy);
                        if (!IsInside(candidate)) continue;
                        float candidateHeight = height[candidate.x, candidate.y] - (continents[candidate.x, candidate.y] < 0.25f ? 0.2f : 0f);
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
        }

        private bool IsInside(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < mapSize && p.y < mapSize;

        private void GenerateBiomes()
        {
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    if (continents[x, y] < 0.25f)
                    {
                        biomes[x, y] = WorldBiome.Ocean;
                    }
                    else if (height[x, y] < 0.42f)
                    {
                        biomes[x, y] = WorldBiome.Beach;
                    }
                    else if (mountains[x, y] > 0.74f)
                    {
                        biomes[x, y] = temperature[x, y] < 0.35f ? WorldBiome.Snow : WorldBiome.Mountain;
                    }
                    else if (temperature[x, y] < 0.28f)
                    {
                        biomes[x, y] = moisture[x, y] < 0.35f ? WorldBiome.Tundra : WorldBiome.Taiga;
                    }
                    else if (temperature[x, y] > 0.68f)
                    {
                        biomes[x, y] = moisture[x, y] < 0.32f ? WorldBiome.Desert : moisture[x, y] > 0.62f ? WorldBiome.Jungle : WorldBiome.Savanna;
                    }
                    else
                    {
                        biomes[x, y] = moisture[x, y] < 0.34f ? WorldBiome.Plains : WorldBiome.Forest;
                    }
                }
            }
        }

        private void RenderPreviewTexture()
        {
            if (generatedTexture == null || generatedTexture.width != mapSize || generatedTexture.height != mapSize)
            {
                generatedTexture = new Texture2D(mapSize, mapSize, TextureFormat.RGBA32, false)
                {
                    name = "Generated World Map",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
            }

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    generatedTexture.SetPixel(x, y, GetPreviewColor(x, y));
                }
            }

            generatedTexture.Apply();
            Renderer renderer = GetComponent<Renderer>();
            Material material = targetMaterial != null ? targetMaterial : renderer.sharedMaterial;
            if (material != null)
            {
                material.mainTexture = generatedTexture;
            }
        }

        private Color GetPreviewColor(int x, int y)
        {
            if (rivers[x, y] && (preview == WorldMapPreview.Biomes || preview == WorldMapPreview.Rivers)) return new Color(0.08f, 0.32f, 0.9f);
            switch (preview)
            {
                case WorldMapPreview.Height: return Color.Lerp(Color.black, Color.white, height[x, y]);
                case WorldMapPreview.Continents: return Color.Lerp(Color.black, Color.white, continents[x, y]);
                case WorldMapPreview.Mountains: return Color.Lerp(Color.black, Color.white, mountains[x, y]);
                case WorldMapPreview.Hills: return Color.Lerp(Color.black, Color.white, hills[x, y]);
                case WorldMapPreview.Temperature: return Color.Lerp(Color.blue, Color.red, temperature[x, y]);
                case WorldMapPreview.Moisture: return Color.Lerp(new Color(0.45f, 0.28f, 0.12f), Color.cyan, moisture[x, y]);
                case WorldMapPreview.Rivers: return rivers[x, y] ? Color.blue : Color.black;
                default: return BiomeColor(biomes[x, y]);
            }
        }

        private static Color BiomeColor(WorldBiome biome)
        {
            switch (biome)
            {
                case WorldBiome.Ocean: return new Color(0.02f, 0.13f, 0.42f);
                case WorldBiome.Beach: return new Color(0.82f, 0.72f, 0.42f);
                case WorldBiome.Tundra: return new Color(0.68f, 0.72f, 0.66f);
                case WorldBiome.Taiga: return new Color(0.18f, 0.36f, 0.28f);
                case WorldBiome.Plains: return new Color(0.45f, 0.66f, 0.24f);
                case WorldBiome.Forest: return new Color(0.12f, 0.45f, 0.18f);
                case WorldBiome.Savanna: return new Color(0.74f, 0.62f, 0.28f);
                case WorldBiome.Desert: return new Color(0.86f, 0.68f, 0.32f);
                case WorldBiome.Jungle: return new Color(0.03f, 0.36f, 0.12f);
                case WorldBiome.Mountain: return new Color(0.42f, 0.39f, 0.35f);
                case WorldBiome.Snow: return Color.white;
                default: return Color.magenta;
            }
        }

        private Vector2 NormalizedCoordinate(int x, int y) => new Vector2(x / (float)(mapSize - 1), y / (float)(mapSize - 1));

        private static float VoronoiNoise(Vector2 point, int noiseSeed)
        {
            Vector2Int cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            float nearest = float.MaxValue;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector2Int neighbour = new Vector2Int(cell.x + x, cell.y + y);
                    Vector2 feature = neighbour + Hash2(neighbour.x, neighbour.y, noiseSeed);
                    nearest = Mathf.Min(nearest, Vector2.Distance(point, feature));
                }
            }

            return Mathf.Clamp01(1f - nearest);
        }

        private static Vector2 Hash2(int x, int y, int hashSeed)
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + hashSeed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            float a = (h & 0xffff) / 65535f;
            float b = ((h >> 16) & 0xffff) / 65535f;
            return new Vector2(a, b);
        }
    }
}
