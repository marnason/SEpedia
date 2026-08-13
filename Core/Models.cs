using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage;
using VRage.Game;
using VRageMath;

namespace SEpedia.Core
{
    [Flags]
    public enum DefinitionCategory
    {
        None = 0,
        PhysicalItem = 1,
        Component = 2,
        Ore = 4,
        Ingot = 8,
        CubeBlock = 16,
        Blueprint = 32
    }

    public enum BrowseCategory
    {
        None = 0,
        Components = 1,
        Ores = 2,
        Ingots = 3,
        Ammo = 4,
        ToolsAndWeapons = 5,
        Consumables = 6,
        GasBottles = 7,
        Items = 8,
        Blocks = 9,
        Celestial = 10
    }

    public enum TriStateFilter
    {
        Either = 0,
        Yes = 1,
        No = 2
    }

    public sealed class DefinitionOrigin
    {
        public bool IsVanilla { get; private set; }
        public string ModName { get; private set; }
        public string ModId { get; private set; }
        public string ServiceName { get; private set; }
        public string SourceFile { get; private set; }

        public string DisplayName
        {
            get
            {
                if (IsVanilla)
                    return "Vanilla";

                if (!string.IsNullOrWhiteSpace(ModName))
                    return ModName;

                if (!string.IsNullOrWhiteSpace(ModId))
                    return ModId;

                return "Unknown origin";
            }
        }

        public string SourceKey
        {
            get
            {
                if (IsVanilla)
                    return "vanilla";

                if (!string.IsNullOrWhiteSpace(ModId))
                    return "mod:" + ServiceName + ":" + ModId;

                if (!string.IsNullOrWhiteSpace(ModName))
                    return "mod-name:" + ModName;

                return "unknown";
            }
        }

        public DefinitionOrigin(bool isVanilla, string modName, string modId, string serviceName, string sourceFile)
        {
            IsVanilla = isVanilla;
            ModName = modName ?? string.Empty;
            ModId = modId ?? string.Empty;
            ServiceName = serviceName ?? string.Empty;
            SourceFile = sourceFile ?? string.Empty;
        }

        public static DefinitionOrigin Unknown
        {
            get { return new DefinitionOrigin(false, string.Empty, string.Empty, string.Empty, string.Empty); }
        }
    }

    public sealed class PhysicalItemData
    {
        public float Mass { get; private set; }
        public float Volume { get; private set; }
        public MyFixedPoint MaxStackAmount { get; private set; }
        public bool HasIntegralAmounts { get; private set; }

        public PhysicalItemData(float mass, float volume, MyFixedPoint maxStackAmount, bool hasIntegralAmounts)
        {
            Mass = mass;
            Volume = volume;
            MaxStackAmount = maxStackAmount;
            HasIntegralAmounts = hasIntegralAmounts;
        }
    }

    public sealed class DefinitionAmount
    {
        public MyDefinitionId DefinitionId { get; private set; }
        public MyFixedPoint Amount { get; private set; }

        public DefinitionAmount(MyDefinitionId definitionId, MyFixedPoint amount)
        {
            DefinitionId = definitionId;
            Amount = amount;
        }
    }

    public sealed class RecipeDocument
    {
        public MyDefinitionId DefinitionId { get; private set; }
        public float BaseProductionTimeSeconds { get; private set; }
        public bool Atomic { get; private set; }
        public IReadOnlyList<DefinitionAmount> Prerequisites { get; private set; }
        public IReadOnlyList<DefinitionAmount> Results { get; private set; }

        public RecipeDocument(
            MyDefinitionId definitionId,
            float baseProductionTimeSeconds,
            bool atomic,
            IList<DefinitionAmount> prerequisites,
            IList<DefinitionAmount> results)
        {
            DefinitionId = definitionId;
            BaseProductionTimeSeconds = baseProductionTimeSeconds;
            Atomic = atomic;
            Prerequisites = new List<DefinitionAmount>(prerequisites).AsReadOnly();
            Results = new List<DefinitionAmount>(results).AsReadOnly();
        }
    }

    public sealed class BlockComponentRequirement
    {
        public MyDefinitionId ComponentId { get; private set; }
        public int Count { get; private set; }

        public BlockComponentRequirement(MyDefinitionId componentId, int count)
        {
            ComponentId = componentId;
            Count = count;
        }
    }

    public sealed class CubeBlockData
    {
        public MyCubeSize CubeSize { get; private set; }
        public Vector3I Size { get; private set; }
        public int Pcu { get; private set; }
        public bool GuiVisible { get; private set; }
        public bool BuildMenuReachable { get; private set; }
        public string BlockPairName { get; private set; }
        public IReadOnlyList<MyDefinitionId> RelatedBlocks { get; private set; }
        public IReadOnlyList<BlockComponentRequirement> Components { get; private set; }

        public CubeBlockData(
            MyCubeSize cubeSize,
            Vector3I size,
            int pcu,
            bool guiVisible,
            bool buildMenuReachable,
            string blockPairName,
            IList<MyDefinitionId> relatedBlocks,
            IList<BlockComponentRequirement> components)
        {
            CubeSize = cubeSize;
            Size = size;
            Pcu = pcu;
            GuiVisible = guiVisible;
            BuildMenuReachable = buildMenuReachable;
            BlockPairName = blockPairName ?? string.Empty;
            RelatedBlocks = new List<MyDefinitionId>(relatedBlocks).AsReadOnly();
            Components = new List<BlockComponentRequirement>(components).AsReadOnly();
        }
    }

    public sealed class PlanetOreData
    {
        public string Material { get; private set; }
        public float Start { get; private set; }
        public float Depth { get; private set; }

        public PlanetOreData(string material, float start, float depth)
        {
            Material = material ?? string.Empty;
            Start = start;
            Depth = depth;
        }
    }

    public sealed class PlanetGeneratorData
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

    public sealed class AsteroidGeneratorData
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

    public sealed class BlockUsage
    {
        public MyDefinitionId BlockId { get; private set; }
        public int Count { get; private set; }

        public BlockUsage(MyDefinitionId blockId, int count)
        {
            BlockId = blockId;
            Count = count;
        }
    }

    public sealed class DefinitionDocument
    {
        public MyDefinitionId Id { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public string RuntimeTypeName { get; private set; }
        public DefinitionCategory Categories { get; private set; }
        public BrowseCategory BrowseCategory { get; private set; }
        public DefinitionOrigin Origin { get; private set; }
        public bool Enabled { get; private set; }
        public bool Public { get; private set; }
        public bool AvailableInSurvival { get; private set; }
        public PhysicalItemData PhysicalItem { get; private set; }
        public RecipeDocument Recipe { get; private set; }
        public CubeBlockData CubeBlock { get; private set; }
        public PlanetGeneratorData PlanetGenerator { get; private set; }
        public AsteroidGeneratorData AsteroidGenerator { get; private set; }

        public string SubtypeName
        {
            get { return Id.SubtypeName; }
        }

        public DefinitionDocument(
            MyDefinitionId id,
            string displayName,
            string description,
            string runtimeTypeName,
            DefinitionCategory categories,
            BrowseCategory browseCategory,
            DefinitionOrigin origin,
            bool enabled,
            bool isPublic,
            bool availableInSurvival,
            PhysicalItemData physicalItem,
            RecipeDocument recipe,
            CubeBlockData cubeBlock,
            PlanetGeneratorData planetGenerator,
            AsteroidGeneratorData asteroidGenerator)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            RuntimeTypeName = runtimeTypeName;
            Categories = categories;
            BrowseCategory = browseCategory;
            Origin = origin;
            Enabled = enabled;
            Public = isPublic;
            AvailableInSurvival = availableInSurvival;
            PhysicalItem = physicalItem;
            Recipe = recipe;
            CubeBlock = cubeBlock;
            PlanetGenerator = planetGenerator;
            AsteroidGenerator = asteroidGenerator;
        }
    }

    public sealed class SearchResult
    {
        public IReadOnlyList<DefinitionDocument> Items { get; private set; }
        public int TotalCount { get; private set; }

        public SearchResult(IList<DefinitionDocument> items, int totalCount)
        {
            Items = new List<DefinitionDocument>(items).AsReadOnly();
            TotalCount = totalCount;
        }
    }
}
