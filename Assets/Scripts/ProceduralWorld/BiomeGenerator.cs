namespace MapGenerator.ProceduralWorld
{
    public readonly struct BiomeClassificationSettings
    {
        public readonly float OceanContinentThreshold;
        public readonly float BeachHeightThreshold;
        public readonly float MountainThreshold;
        public readonly float SnowTemperatureThreshold;
        public readonly float ColdTemperatureThreshold;
        public readonly float ColdDryMoistureThreshold;
        public readonly float HotTemperatureThreshold;
        public readonly float HotDryMoistureThreshold;
        public readonly float HotWetMoistureThreshold;
        public readonly float TemperateDryMoistureThreshold;

        public BiomeClassificationSettings(
            float oceanContinentThreshold,
            float beachHeightThreshold,
            float mountainThreshold,
            float snowTemperatureThreshold,
            float coldTemperatureThreshold,
            float coldDryMoistureThreshold,
            float hotTemperatureThreshold,
            float hotDryMoistureThreshold,
            float hotWetMoistureThreshold,
            float temperateDryMoistureThreshold)
        {
            OceanContinentThreshold = oceanContinentThreshold;
            BeachHeightThreshold = beachHeightThreshold;
            MountainThreshold = mountainThreshold;
            SnowTemperatureThreshold = snowTemperatureThreshold;
            ColdTemperatureThreshold = coldTemperatureThreshold;
            ColdDryMoistureThreshold = coldDryMoistureThreshold;
            HotTemperatureThreshold = hotTemperatureThreshold;
            HotDryMoistureThreshold = hotDryMoistureThreshold;
            HotWetMoistureThreshold = hotWetMoistureThreshold;
            TemperateDryMoistureThreshold = temperateDryMoistureThreshold;
        }
    }

    public static class BiomeGenerator
    {
        public static WorldBiome ClassifyBiome(
            float continent,
            float height,
            float mountain,
            float temperature,
            float moisture,
            BiomeClassificationSettings settings)
        {
            if (continent < settings.OceanContinentThreshold)
            {
                return WorldBiome.Ocean;
            }

            if (height < settings.BeachHeightThreshold)
            {
                return WorldBiome.Beach;
            }

            if (mountain > settings.MountainThreshold)
            {
                return temperature < settings.SnowTemperatureThreshold ? WorldBiome.Snow : WorldBiome.Mountain;
            }

            if (temperature < settings.ColdTemperatureThreshold)
            {
                return moisture < settings.ColdDryMoistureThreshold ? WorldBiome.Tundra : WorldBiome.Taiga;
            }

            if (temperature > settings.HotTemperatureThreshold)
            {
                return moisture < settings.HotDryMoistureThreshold
                    ? WorldBiome.Desert
                    : moisture > settings.HotWetMoistureThreshold
                        ? WorldBiome.Jungle
                        : WorldBiome.Savanna;
            }

            return moisture < settings.TemperateDryMoistureThreshold ? WorldBiome.Plains : WorldBiome.Forest;
        }
    }
}
