using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage;
using VRage.Game;
using VRageMath;

namespace SEpedia.Core
{
    [Flags]
    internal enum DefinitionCategory
    {
        None = 0,
        PhysicalItem = 1,
        Component = 2,
        Ore = 4,
        Ingot = 8,
        CubeBlock = 16,
        Blueprint = 32
    }

    internal enum BrowseCategory
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
        Recipes = 10,
        Celestial = 11
    }

    internal sealed class DefinitionOrigin
    {
        private static readonly DefinitionOrigin UnknownValue =
            new DefinitionOrigin(false, string.Empty, string.Empty, string.Empty, string.Empty);

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

        public static DefinitionOrigin Unknown
        {
            get { return UnknownValue; }
        }

        public DefinitionOrigin(bool isVanilla, string modName, string modId, string serviceName, string sourceFile)
        {
            IsVanilla = isVanilla;
            ModName = modName ?? string.Empty;
            ModId = modId ?? string.Empty;
            ServiceName = serviceName ?? string.Empty;
            SourceFile = sourceFile ?? string.Empty;
        }
    }

    internal sealed class PhysicalItemData
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

    internal sealed class BlockComponentRequirement
    {
        public MyDefinitionId ComponentId { get; private set; }
        public int Count { get; private set; }

        public BlockComponentRequirement(MyDefinitionId componentId, int count)
        {
            ComponentId = componentId;
            Count = count;
        }
    }

    internal sealed class CubeBlockData
    {
        public MyCubeSize CubeSize { get; private set; }
        public Vector3I Size { get; private set; }
        public int Pcu { get; private set; }
        public bool IsGuiVisible { get; private set; }
        public bool IsBuildMenuReachable { get; private set; }
        public string BlockPairName { get; private set; }
        public IReadOnlyList<MyDefinitionId> RelatedBlocks { get; private set; }
        public IReadOnlyList<BlockComponentRequirement> Components { get; private set; }

        public CubeBlockData(
            MyCubeSize cubeSize,
            Vector3I size,
            int pcu,
            bool isGuiVisible,
            bool isBuildMenuReachable,
            string blockPairName,
            IList<MyDefinitionId> relatedBlocks,
            IList<BlockComponentRequirement> components)
        {
            CubeSize = cubeSize;
            Size = size;
            Pcu = pcu;
            IsGuiVisible = isGuiVisible;
            IsBuildMenuReachable = isBuildMenuReachable;
            BlockPairName = blockPairName ?? string.Empty;
            RelatedBlocks = new List<MyDefinitionId>(relatedBlocks).AsReadOnly();
            Components = new List<BlockComponentRequirement>(components).AsReadOnly();
        }
    }

    internal sealed class BlockUsage
    {
        public MyDefinitionId BlockId { get; private set; }
        public int Count { get; private set; }

        public BlockUsage(MyDefinitionId blockId, int count)
        {
            BlockId = blockId;
            Count = count;
        }
    }

    internal sealed class DefinitionDocument
    {
        public MyDefinitionId Id { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public string RuntimeTypeName { get; private set; }
        public DefinitionCategory Categories { get; private set; }
        public BrowseCategory BrowseCategory { get; private set; }
        public DefinitionOrigin Origin { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool IsPublic { get; private set; }
        public bool IsAvailableInSurvival { get; private set; }
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
            bool isEnabled,
            bool isPublic,
            bool isAvailableInSurvival,
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
            IsEnabled = isEnabled;
            IsPublic = isPublic;
            IsAvailableInSurvival = isAvailableInSurvival;
            PhysicalItem = physicalItem;
            Recipe = recipe;
            CubeBlock = cubeBlock;
            PlanetGenerator = planetGenerator;
            AsteroidGenerator = asteroidGenerator;
        }
    }
}
