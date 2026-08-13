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
        public IReadOnlyList<BlockComponentRequirement> Components { get; private set; }

        public CubeBlockData(MyCubeSize cubeSize, Vector3I size, int pcu, IList<BlockComponentRequirement> components)
        {
            CubeSize = cubeSize;
            Size = size;
            Pcu = pcu;
            Components = new List<BlockComponentRequirement>(components).AsReadOnly();
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
        public DefinitionOrigin Origin { get; private set; }
        public bool Enabled { get; private set; }
        public bool Public { get; private set; }
        public bool AvailableInSurvival { get; private set; }
        public PhysicalItemData PhysicalItem { get; private set; }
        public RecipeDocument Recipe { get; private set; }
        public CubeBlockData CubeBlock { get; private set; }

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
            DefinitionOrigin origin,
            bool enabled,
            bool isPublic,
            bool availableInSurvival,
            PhysicalItemData physicalItem,
            RecipeDocument recipe,
            CubeBlockData cubeBlock)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            RuntimeTypeName = runtimeTypeName;
            Categories = categories;
            Origin = origin;
            Enabled = enabled;
            Public = isPublic;
            AvailableInSurvival = availableInSurvival;
            PhysicalItem = physicalItem;
            Recipe = recipe;
            CubeBlock = cubeBlock;
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
