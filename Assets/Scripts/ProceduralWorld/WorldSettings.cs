using UnityEngine;

namespace MapGenerator.ProceduralWorld
{
    [CreateAssetMenu(fileName = "WorldSettings", menuName = "Map Generator/Procedural World Settings")]
    public sealed class WorldSettings : ScriptableObject
    {
        [Header("Preview")]
        [SerializeField, Min(16)] private int mapSize = 256;
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

        [Header("Biome Thresholds")]
        [SerializeField, Range(0f, 1f)] private float oceanContinentThreshold = 0.25f;
        [SerializeField, Range(0f, 1f)] private float beachHeightThreshold = 0.42f;
        [SerializeField, Range(0f, 1f)] private float mountainThreshold = 0.74f;
        [SerializeField, Range(0f, 1f)] private float snowTemperatureThreshold = 0.35f;
        [SerializeField, Range(0f, 1f)] private float coldTemperatureThreshold = 0.28f;
        [SerializeField, Range(0f, 1f)] private float coldDryMoistureThreshold = 0.35f;
        [SerializeField, Range(0f, 1f)] private float hotTemperatureThreshold = 0.68f;
        [SerializeField, Range(0f, 1f)] private float hotDryMoistureThreshold = 0.32f;
        [SerializeField, Range(0f, 1f)] private float hotWetMoistureThreshold = 0.62f;
        [SerializeField, Range(0f, 1f)] private float temperateDryMoistureThreshold = 0.34f;

        [Header("Biome Colors")]
        [SerializeField] private Color oceanColor = new Color(0.02f, 0.13f, 0.42f);
        [SerializeField] private Color beachColor = new Color(0.82f, 0.72f, 0.42f);
        [SerializeField] private Color tundraColor = new Color(0.68f, 0.72f, 0.66f);
        [SerializeField] private Color taigaColor = new Color(0.18f, 0.36f, 0.28f);
        [SerializeField] private Color plainsColor = new Color(0.45f, 0.66f, 0.24f);
        [SerializeField] private Color forestColor = new Color(0.12f, 0.45f, 0.18f);
        [SerializeField] private Color savannaColor = new Color(0.74f, 0.62f, 0.28f);
        [SerializeField] private Color desertColor = new Color(0.86f, 0.68f, 0.32f);
        [SerializeField] private Color jungleColor = new Color(0.03f, 0.36f, 0.12f);
        [SerializeField] private Color mountainColor = new Color(0.42f, 0.39f, 0.35f);
        [SerializeField] private Color snowColor = Color.white;

        public int MapSize => Mathf.Max(16, mapSize);
        public int Seed => seed;
        public float VoronoiScale => voronoiScale;
        public float ContinentSimplexScale => continentSimplexScale;
        public float CoastThreshold => coastThreshold;
        public float CoastBlend => coastBlend;
        public float MountainScale => mountainScale;
        public int MountainOctaves => mountainOctaves;
        public float HillScale => hillScale;
        public int HillOctaves => hillOctaves;
        public float TemperatureNoiseScale => temperatureNoiseScale;
        public float TemperatureNoiseStrength => temperatureNoiseStrength;
        public float MoistureScale => moistureScale;
        public float CoastalMoistureStrength => coastalMoistureStrength;
        public int RiverCount => riverCount;
        public int MaxRiverLength => maxRiverLength;
        public float RiverSourceMinHeight => riverSourceMinHeight;
        public float OceanContinentThreshold => oceanContinentThreshold;
        public float BeachHeightThreshold => beachHeightThreshold;
        public float MountainThreshold => mountainThreshold;
        public float SnowTemperatureThreshold => snowTemperatureThreshold;
        public float ColdTemperatureThreshold => coldTemperatureThreshold;
        public float ColdDryMoistureThreshold => coldDryMoistureThreshold;
        public float HotTemperatureThreshold => hotTemperatureThreshold;
        public float HotDryMoistureThreshold => hotDryMoistureThreshold;
        public float HotWetMoistureThreshold => hotWetMoistureThreshold;
        public float TemperateDryMoistureThreshold => temperateDryMoistureThreshold;

        public Color GetBiomeColor(WorldBiome biome)
        {
            switch (biome)
            {
                case WorldBiome.Ocean: return oceanColor;
                case WorldBiome.Beach: return beachColor;
                case WorldBiome.Tundra: return tundraColor;
                case WorldBiome.Taiga: return taigaColor;
                case WorldBiome.Plains: return plainsColor;
                case WorldBiome.Forest: return forestColor;
                case WorldBiome.Savanna: return savannaColor;
                case WorldBiome.Desert: return desertColor;
                case WorldBiome.Jungle: return jungleColor;
                case WorldBiome.Mountain: return mountainColor;
                case WorldBiome.Snow: return snowColor;
                default: return Color.magenta;
            }
        }
    }
}
