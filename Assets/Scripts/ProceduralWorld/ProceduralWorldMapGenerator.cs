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
        Rivers,
        Lakes,
        SpawnMap
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
        [SerializeField] private WorldSettings settings;
        [SerializeField] private WorldMapPreview preview = WorldMapPreview.Biomes;

        [Header("Fallback Settings")]
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
        [SerializeField, Min(0)] private int minRiverLength = 24;
        [SerializeField, Min(0)] private int minRiverTurns = 2;
        [SerializeField, Min(0)] private int maxRiverIntersections = 1;
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
        private bool[,] lakes;
        private WorldBiome[,] biomes;
        private bool[,] spawnMap;
        private Color32[] _previewPixels;
        private bool _needsRegenerate;

        public Texture2D GeneratedTexture => generatedTexture;
        public float[,] ContinentsMap => CopyMap(continents);
        public float[,] MountainsMap => CopyMap(mountains);
        public float[,] HillsMap => CopyMap(hills);
        public float[,] TemperatureMap => CopyMap(temperature);
        public float[,] MoistureMap => CopyMap(moisture);
        public float[,] HeightMap => CopyMap(height);
        public bool[,] RiversMap => CopyMap(rivers);
        public bool[,] LakesMap => CopyMap(lakes);
        public WorldBiome[,] BiomesMap => CopyMap(biomes);
        public bool[,] SpawnMap => CopyMap(spawnMap);
        private int MapSize => settings != null ? settings.MapSize : Mathf.Max(16, mapSize);
        private int Seed => settings != null ? settings.Seed : seed;
        private float VoronoiScale => settings != null ? settings.VoronoiScale : voronoiScale;
        private float ContinentSimplexScale => settings != null ? settings.ContinentSimplexScale : continentSimplexScale;
        private float CoastThreshold => settings != null ? settings.CoastThreshold : coastThreshold;
        private float CoastBlend => settings != null ? settings.CoastBlend : coastBlend;
        private float MountainScale => settings != null ? settings.MountainScale : mountainScale;
        private int MountainOctaves => settings != null ? settings.MountainOctaves : mountainOctaves;
        private float HillScale => settings != null ? settings.HillScale : hillScale;
        private int HillOctaves => settings != null ? settings.HillOctaves : hillOctaves;
        private float TemperatureNoiseScale => settings != null ? settings.TemperatureNoiseScale : temperatureNoiseScale;
        private float TemperatureNoiseStrength => settings != null ? settings.TemperatureNoiseStrength : temperatureNoiseStrength;
        private float MoistureScale => settings != null ? settings.MoistureScale : moistureScale;
        private float CoastalMoistureStrength => settings != null ? settings.CoastalMoistureStrength : coastalMoistureStrength;
        private int RiverCount => settings != null ? settings.RiverCount : riverCount;
        private int MaxRiverLength => settings != null ? settings.MaxRiverLength : maxRiverLength;
        private int MinRiverLength => settings != null ? settings.MinRiverLength : minRiverLength;
        private int MinRiverTurns => settings != null ? settings.MinRiverTurns : minRiverTurns;
        private int MaxRiverIntersections => settings != null ? settings.MaxRiverIntersections : maxRiverIntersections;
        private float RiverSourceMinHeight => settings != null ? settings.RiverSourceMinHeight : riverSourceMinHeight;
        private float OceanContinentThreshold => settings != null ? settings.OceanContinentThreshold : 0.25f;
        private float BeachHeightThreshold => settings != null ? settings.BeachHeightThreshold : 0.42f;
        private float MountainThreshold => settings != null ? settings.MountainThreshold : 0.74f;
        private float SnowTemperatureThreshold => settings != null ? settings.SnowTemperatureThreshold : 0.35f;
        private float ColdTemperatureThreshold => settings != null ? settings.ColdTemperatureThreshold : 0.28f;
        private float ColdDryMoistureThreshold => settings != null ? settings.ColdDryMoistureThreshold : 0.35f;
        private float HotTemperatureThreshold => settings != null ? settings.HotTemperatureThreshold : 0.68f;
        private float HotDryMoistureThreshold => settings != null ? settings.HotDryMoistureThreshold : 0.32f;
        private float HotWetMoistureThreshold => settings != null ? settings.HotWetMoistureThreshold : 0.62f;
        private float TemperateDryMoistureThreshold => settings != null ? settings.TemperateDryMoistureThreshold : 0.34f;

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += HandleEditorUpdate;
#endif
            if (autoRegenerate)
            {
                Generate();
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= HandleEditorUpdate;
#endif
        }

        private void OnValidate()
        {
            mapSize = Mathf.Max(16, mapSize);
            maxRiverLength = Mathf.Max(1, maxRiverLength);
            minRiverLength = Mathf.Max(0, minRiverLength);
            minRiverTurns = Mathf.Max(0, minRiverTurns);
            maxRiverIntersections = Mathf.Max(0, maxRiverIntersections);
            if (autoRegenerate)
            {
                _needsRegenerate = true;
            }
        }

#if UNITY_EDITOR
        private void HandleEditorUpdate()
        {
            if (!_needsRegenerate)
            {
                return;
            }

            _needsRegenerate = false;
            Generate();
        }
#endif

        [ContextMenu("Generate World Map")]
        public void Generate()
        {
            AllocateMaps();
            GenerateContinents();
            GenerateRelief();
            GenerateTemperature();
            GenerateMoisture();
            GenerateRivers();
            GenerateLakes();
            GenerateBiomes();
            GenerateSpawnMap();
            RenderPreviewTexture();
        }

        private void AllocateMaps()
        {
            continents = new float[MapSize, MapSize];
            mountains = new float[MapSize, MapSize];
            hills = new float[MapSize, MapSize];
            temperature = new float[MapSize, MapSize];
            moisture = new float[MapSize, MapSize];
            height = new float[MapSize, MapSize];
            rivers = new bool[MapSize, MapSize];
            lakes = new bool[MapSize, MapSize];
            biomes = new WorldBiome[MapSize, MapSize];
            spawnMap = new bool[MapSize, MapSize];
        }

        private void GenerateContinents()
        {
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float voronoi = VoronoiNoise(uv * VoronoiScale, Seed);
                    float simplex = SimplexNoise.Fractal(uv.x * ContinentSimplexScale, uv.y * ContinentSimplexScale, 4, 0.5f, 2f, Seed + 17);
                    float combined = Mathf.Clamp01(voronoi + 0.35f * simplex);
                    continents[x, y] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(CoastThreshold - CoastBlend, CoastThreshold + CoastBlend, combined));
                }
            }
        }

        private void GenerateRelief()
        {
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float ridge = SimplexNoise.RidgedFractal(uv.x * MountainScale, uv.y * MountainScale, MountainOctaves, Seed + 101);
                    float hill = Mathf.Clamp01(0.5f + 0.5f * SimplexNoise.Fractal(uv.x * HillScale, uv.y * HillScale, HillOctaves, 0.5f, 2f, Seed + 211));
                    mountains[x, y] = ridge * continents[x, y];
                    hills[x, y] = hill * continents[x, y];
                    height[x, y] = Mathf.Clamp01(continents[x, y] * 0.7f + mountains[x, y] * 0.2f + hills[x, y] * 0.1f);
                }
            }
        }

        private void GenerateTemperature()
        {
            for (int y = 0; y < MapSize; y++)
            {
                float latitude = 1f - Mathf.Abs((y / (float)(MapSize - 1)) * 2f - 1f);
                for (int x = 0; x < MapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float noise = SimplexNoise.Noise(uv.x * TemperatureNoiseScale, uv.y * TemperatureNoiseScale, Seed + 307) * TemperatureNoiseStrength;
                    float altitudeCooling = mountains[x, y] * 0.28f;
                    temperature[x, y] = Mathf.Clamp01(latitude + noise - altitudeCooling);
                }
            }
        }

        private void GenerateMoisture()
        {
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    Vector2 uv = NormalizedCoordinate(x, y);
                    float baseMoisture = Mathf.Clamp01(0.5f + 0.5f * SimplexNoise.Fractal(uv.x * MoistureScale, uv.y * MoistureScale, 4, 0.55f, 2f, Seed + 409));
                    float waterInfluence = 1f - continents[x, y];
                    float coastInfluence = EstimateCoastalMoisture(x, y);
                    moisture[x, y] = Mathf.Clamp01(baseMoisture + waterInfluence + coastInfluence * CoastalMoistureStrength);
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
                    int sx = Mathf.Clamp(x + ox, 0, MapSize - 1);
                    int sy = Mathf.Clamp(y + oy, 0, MapSize - 1);
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
            System.Random random = new System.Random(Seed + 503);
            for (int i = 0; i < RiverCount; i++)
            {
                Vector2Int source = FindRiverSource(random);
                TraceRiver(source);
            }
        }

        private Vector2Int FindRiverSource(System.Random random)
        {
            Vector2Int best = new Vector2Int(random.Next(MapSize), random.Next(MapSize));
            float bestScore = -1f;
            for (int attempt = 0; attempt < 128; attempt++)
            {
                int x = random.Next(MapSize);
                int y = random.Next(MapSize);
                float score = height[x, y] + mountains[x, y] * 0.5f;
                if (height[x, y] >= RiverSourceMinHeight && score > bestScore)
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
            List<Vector2Int> path = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            bool reachedOcean = false;
            bool reachedRiver = false;
            int intersections = 0;

            for (int step = 0; step < MaxRiverLength; step++)
            {
                if (!IsInside(current))
                {
                    break;
                }

                if (continents[current.x, current.y] < OceanContinentThreshold)
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
                        if (!IsInside(candidate)) continue;
                        float candidateHeight = height[candidate.x, candidate.y] - (continents[candidate.x, candidate.y] < OceanContinentThreshold ? 0.2f : 0f);
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

            if (path.Count < MinRiverLength || CountRiverTurns(path) < MinRiverTurns || intersections > MaxRiverIntersections || (!reachedOcean && !reachedRiver))
            {
                return;
            }

            foreach (Vector2Int riverTile in path)
            {
                rivers[riverTile.x, riverTile.y] = true;
                moisture[riverTile.x, riverTile.y] = 1f;
            }
        }

        private static int CountRiverTurns(List<Vector2Int> path)
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

        private bool IsInside(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < MapSize && p.y < MapSize;

        private void GenerateLakes()
        {
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    if (continents[x, y] < OceanContinentThreshold || rivers[x, y])
                    {
                        continue;
                    }

                    bool localBasin = IsLocalBasin(x, y);
                    bool wetLowland = moisture[x, y] > 0.82f && height[x, y] < 0.58f && mountains[x, y] < MountainThreshold;
                    if (localBasin || wetLowland)
                    {
                        lakes[x, y] = true;
                        moisture[x, y] = 1f;
                    }
                }
            }
        }

        private bool IsLocalBasin(int x, int y)
        {
            if (height[x, y] >= 0.62f || moisture[x, y] < 0.58f)
            {
                return false;
            }

            float currentHeight = height[x, y];
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0) continue;
                    int sx = Mathf.Clamp(x + ox, 0, MapSize - 1);
                    int sy = Mathf.Clamp(y + oy, 0, MapSize - 1);
                    if (height[sx, sy] < currentHeight || continents[sx, sy] < OceanContinentThreshold)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void GenerateBiomes()
        {
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    if (continents[x, y] < OceanContinentThreshold)
                    {
                        biomes[x, y] = WorldBiome.Ocean;
                    }
                    else if (height[x, y] < BeachHeightThreshold)
                    {
                        biomes[x, y] = WorldBiome.Beach;
                    }
                    else if (mountains[x, y] > MountainThreshold)
                    {
                        biomes[x, y] = temperature[x, y] < SnowTemperatureThreshold ? WorldBiome.Snow : WorldBiome.Mountain;
                    }
                    else if (temperature[x, y] < ColdTemperatureThreshold)
                    {
                        biomes[x, y] = moisture[x, y] < ColdDryMoistureThreshold ? WorldBiome.Tundra : WorldBiome.Taiga;
                    }
                    else if (temperature[x, y] > HotTemperatureThreshold)
                    {
                        biomes[x, y] = moisture[x, y] < HotDryMoistureThreshold ? WorldBiome.Desert : moisture[x, y] > HotWetMoistureThreshold ? WorldBiome.Jungle : WorldBiome.Savanna;
                    }
                    else
                    {
                        biomes[x, y] = moisture[x, y] < TemperateDryMoistureThreshold ? WorldBiome.Plains : WorldBiome.Forest;
                    }
                }
            }
        }


        private void GenerateSpawnMap()
        {
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    spawnMap[x, y] = IsSpawnCandidate(x, y);
                }
            }
        }

        private bool IsSpawnCandidate(int x, int y)
        {
            if (continents[x, y] < OceanContinentThreshold || rivers[x, y] || lakes[x, y])
            {
                return false;
            }

            if (height[x, y] < BeachHeightThreshold || mountains[x, y] > MountainThreshold)
            {
                return false;
            }

            switch (biomes[x, y])
            {
                case WorldBiome.Plains:
                case WorldBiome.Forest:
                case WorldBiome.Savanna:
                case WorldBiome.Taiga:
                    return true;
                default:
                    return false;
            }
        }

        private void RenderPreviewTexture()
        {
            if (generatedTexture == null || generatedTexture.width != MapSize || generatedTexture.height != MapSize)
            {
                generatedTexture = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false)
                {
                    name = "Generated World Map",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };
            }

            int pixelCount = MapSize * MapSize;
            if (_previewPixels == null || _previewPixels.Length != pixelCount)
            {
                _previewPixels = new Color32[pixelCount];
            }

            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    _previewPixels[x + y * MapSize] = GetPreviewColor(x, y);
                }
            }

            generatedTexture.SetPixels32(_previewPixels);
            generatedTexture.Apply(false);
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
            if (lakes[x, y] && (preview == WorldMapPreview.Biomes || preview == WorldMapPreview.Lakes)) return new Color(0.05f, 0.45f, 0.8f);
            if (spawnMap[x, y] && preview == WorldMapPreview.SpawnMap) return new Color(0.1f, 0.9f, 0.2f);
            switch (preview)
            {
                case WorldMapPreview.Height: return Color.Lerp(Color.black, Color.white, height[x, y]);
                case WorldMapPreview.Continents: return Color.Lerp(Color.black, Color.white, continents[x, y]);
                case WorldMapPreview.Mountains: return Color.Lerp(Color.black, Color.white, mountains[x, y]);
                case WorldMapPreview.Hills: return Color.Lerp(Color.black, Color.white, hills[x, y]);
                case WorldMapPreview.Temperature: return Color.Lerp(Color.blue, Color.red, temperature[x, y]);
                case WorldMapPreview.Moisture: return Color.Lerp(new Color(0.45f, 0.28f, 0.12f), Color.cyan, moisture[x, y]);
                case WorldMapPreview.Rivers: return rivers[x, y] ? Color.blue : Color.black;
                case WorldMapPreview.Lakes: return lakes[x, y] ? Color.cyan : Color.black;
                case WorldMapPreview.SpawnMap: return spawnMap[x, y] ? Color.green : Color.black;
                default: return BiomeColor(biomes[x, y]);
            }
        }

        private Color BiomeColor(WorldBiome biome)
        {
            if (settings != null)
            {
                return settings.GetBiomeColor(biome);
            }

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


        private static T[,] CopyMap<T>(T[,] source)
        {
            return source == null ? null : (T[,])source.Clone();
        }

        private Vector2 NormalizedCoordinate(int x, int y) => new Vector2(x / (float)(MapSize - 1), y / (float)(MapSize - 1));

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
