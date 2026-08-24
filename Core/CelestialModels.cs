using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace SEpedia.Core
{
    internal sealed class PlanetVoxelMaterialData
    {
        public string SubtypeName { get; private set; }
        public string DisplayName { get; private set; }
        public string MinedOre { get; private set; }
        public float MinedOreRatio { get; private set; }
        public bool CanBeHarvested { get; private set; }
        public bool IsRare { get; private set; }
        public bool SpawnsInAsteroids { get; private set; }
        public bool SpawnsFromMeteorites { get; private set; }
        public int AsteroidSpawnProbabilityMultiplier { get; private set; }

        public PlanetVoxelMaterialData(
            string subtypeName,
            string displayName,
            string minedOre,
            float minedOreRatio,
            bool canBeHarvested,
            bool isRare,
            bool spawnsInAsteroids,
            bool spawnsFromMeteorites,
            int asteroidSpawnProbabilityMultiplier)
        {
            SubtypeName = subtypeName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            MinedOre = minedOre ?? string.Empty;
            MinedOreRatio = minedOreRatio;
            CanBeHarvested = canBeHarvested;
            IsRare = isRare;
            SpawnsInAsteroids = spawnsInAsteroids;
            SpawnsFromMeteorites = spawnsFromMeteorites;
            AsteroidSpawnProbabilityMultiplier = asteroidSpawnProbabilityMultiplier;
        }
    }

    internal sealed class PlanetOreData
    {
        public string Material { get; private set; }
        public MyDefinitionId? OreId { get; private set; }
        public byte Value { get; private set; }
        public float Start { get; private set; }
        public float Depth { get; private set; }
        public string TargetColor { get; private set; }
        public float ColorInfluence { get; private set; }
        public string ColorShift { get; private set; }
        public PlanetVoxelMaterialData VoxelMaterial { get; private set; }

        public PlanetOreData(
            string material,
            MyDefinitionId? oreId,
            byte value,
            float start,
            float depth,
            string targetColor,
            float colorInfluence,
            string colorShift,
            PlanetVoxelMaterialData voxelMaterial)
        {
            Material = material ?? string.Empty;
            OreId = oreId;
            Value = value;
            Start = start;
            Depth = depth;
            TargetColor = targetColor ?? string.Empty;
            ColorInfluence = colorInfluence;
            ColorShift = colorShift ?? string.Empty;
            VoxelMaterial = voxelMaterial;
        }
    }

    internal sealed class PlanetOreUsage
    {
        public MyDefinitionId GeneratorId { get; private set; }
        public PlanetOreData Mapping { get; private set; }

        public PlanetOreUsage(MyDefinitionId generatorId, PlanetOreData mapping)
        {
            GeneratorId = generatorId;
            Mapping = mapping;
        }
    }

    internal sealed class PlanetGeneratorData
    {
        public float SurfaceGravity { get; private set; }
        public float GravityFalloffPower { get; private set; }
        public bool HasAtmosphere { get; private set; }
        public float AtmosphereHeight { get; private set; }
        public bool AtmosphereBreathable { get; private set; }
        public float AtmosphereDensity { get; private set; }
        public float OxygenDensity { get; private set; }
        public float AtmosphereLimitAltitude { get; private set; }
        public float MaxWindSpeed { get; private set; }
        public string DefaultTemperature { get; private set; }
        public int WeatherFrequencyMin { get; private set; }
        public int WeatherFrequencyMax { get; private set; }
        public string PersistentWeather { get; private set; }
        public IReadOnlyList<string> WeatherTypes { get; private set; }
        public IReadOnlyList<PlanetOreData> Ores { get; private set; }

        public PlanetGeneratorData(
            float surfaceGravity,
            float gravityFalloffPower,
            bool hasAtmosphere,
            float atmosphereHeight,
            bool atmosphereBreathable,
            float atmosphereDensity,
            float oxygenDensity,
            float atmosphereLimitAltitude,
            float maxWindSpeed,
            string defaultTemperature,
            int weatherFrequencyMin,
            int weatherFrequencyMax,
            string persistentWeather,
            IList<string> weatherTypes,
            IList<PlanetOreData> ores)
        {
            SurfaceGravity = surfaceGravity;
            GravityFalloffPower = gravityFalloffPower;
            HasAtmosphere = hasAtmosphere;
            AtmosphereHeight = atmosphereHeight;
            AtmosphereBreathable = atmosphereBreathable;
            AtmosphereDensity = atmosphereDensity;
            OxygenDensity = oxygenDensity;
            AtmosphereLimitAltitude = atmosphereLimitAltitude;
            MaxWindSpeed = maxWindSpeed;
            DefaultTemperature = defaultTemperature ?? string.Empty;
            WeatherFrequencyMin = weatherFrequencyMin;
            WeatherFrequencyMax = weatherFrequencyMax;
            PersistentWeather = persistentWeather ?? string.Empty;
            WeatherTypes = new List<string>(weatherTypes).AsReadOnly();
            Ores = new List<PlanetOreData>(ores).AsReadOnly();
        }
    }

    internal sealed class AsteroidGeneratorData
    {
        public int Version { get; private set; }
        public int ObjectSizeMin { get; private set; }
        public int ObjectSizeMax { get; private set; }
        public int ClusterObjectSizeMin { get; private set; }
        public int ClusterObjectSizeMax { get; private set; }
        public int MaxObjectsInCluster { get; private set; }
        public int MinClusterDistance { get; private set; }
        public int MaxClusterDistanceMin { get; private set; }
        public int MaxClusterDistanceMax { get; private set; }
        public double ClusterDensity { get; private set; }
        public bool AbsoluteClusterDispersion { get; private set; }
        public bool RotateAsteroids { get; private set; }
        public bool VariableClusterSize { get; private set; }
        public IReadOnlyList<string> SeedProbabilities { get; private set; }
        public IReadOnlyList<string> ClusterSeedProbabilities { get; private set; }

        public AsteroidGeneratorData(
            int version,
            int objectSizeMin,
            int objectSizeMax,
            int clusterObjectSizeMin,
            int clusterObjectSizeMax,
            int maxObjectsInCluster,
            int minClusterDistance,
            int maxClusterDistanceMin,
            int maxClusterDistanceMax,
            double clusterDensity,
            bool absoluteClusterDispersion,
            bool rotateAsteroids,
            bool variableClusterSize,
            IList<string> seedProbabilities,
            IList<string> clusterSeedProbabilities)
        {
            Version = version;
            ObjectSizeMin = objectSizeMin;
            ObjectSizeMax = objectSizeMax;
            ClusterObjectSizeMin = clusterObjectSizeMin;
            ClusterObjectSizeMax = clusterObjectSizeMax;
            MaxObjectsInCluster = maxObjectsInCluster;
            MinClusterDistance = minClusterDistance;
            MaxClusterDistanceMin = maxClusterDistanceMin;
            MaxClusterDistanceMax = maxClusterDistanceMax;
            ClusterDensity = clusterDensity;
            AbsoluteClusterDispersion = absoluteClusterDispersion;
            RotateAsteroids = rotateAsteroids;
            VariableClusterSize = variableClusterSize;
            SeedProbabilities = new List<string>(seedProbabilities).AsReadOnly();
            ClusterSeedProbabilities = new List<string>(clusterSeedProbabilities).AsReadOnly();
        }
    }

    internal sealed class PlanetSnapshot
    {
        public long EntityId { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3D Position { get; private set; }
        public float MinimumRadius { get; private set; }
        public float AverageRadius { get; private set; }
        public float MaximumRadius { get; private set; }
        public bool HasAtmosphere { get; private set; }
        public float AtmosphereRadius { get; private set; }
        public float AtmosphereAltitude { get; private set; }
        public MyDefinitionId? GeneratorId { get; private set; }
        public DefinitionOrigin Origin { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool IsPublic { get; private set; }
        public bool IsAvailableInSurvival { get; private set; }
        public bool HasGeneratorMetadata { get; private set; }

        public PlanetSnapshot(
            long entityId,
            string displayName,
            Vector3D position,
            float minimumRadius,
            float averageRadius,
            float maximumRadius,
            bool hasAtmosphere,
            float atmosphereRadius,
            float atmosphereAltitude,
            MyDefinitionId? generatorId,
            DefinitionOrigin origin,
            bool isEnabled,
            bool isPublic,
            bool isAvailableInSurvival,
            bool hasGeneratorMetadata)
        {
            EntityId = entityId;
            DisplayName = displayName ?? string.Empty;
            Position = position;
            MinimumRadius = minimumRadius;
            AverageRadius = averageRadius;
            MaximumRadius = maximumRadius;
            HasAtmosphere = hasAtmosphere;
            AtmosphereRadius = atmosphereRadius;
            AtmosphereAltitude = atmosphereAltitude;
            GeneratorId = generatorId;
            Origin = origin ?? DefinitionOrigin.Unknown;
            IsEnabled = isEnabled;
            IsPublic = isPublic;
            IsAvailableInSurvival = isAvailableInSurvival;
            HasGeneratorMetadata = hasGeneratorMetadata;
        }
    }
}
