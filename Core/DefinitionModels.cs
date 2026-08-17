using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace SEpedia.Core
{
    internal enum BrowseCategory
    {
        None = 0,
        Components = 1,
        Ores = 2,
        Ingots = 3,
        Ammo = 4,
        ToolsAndWeapons = 5,
        Consumables = 6,
        Items = 8,
        Blocks = 9,
        Recipes = 10,
        Celestial = 11
    }

    internal sealed class DefinitionOrigin
    {
        #region State

        private static readonly DefinitionOrigin UnknownValue =
            new DefinitionOrigin(false, string.Empty, string.Empty, string.Empty);

        public bool IsVanilla { get; private set; }
        public string ModName { get; private set; }
        public string ModId { get; private set; }
        public string ServiceName { get; private set; }

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

        #endregion

        #region Construction

        public DefinitionOrigin(bool isVanilla, string modName, string modId, string serviceName)
        {
            IsVanilla = isVanilla;
            ModName = modName ?? string.Empty;
            ModId = modId ?? string.Empty;
            ServiceName = serviceName ?? string.Empty;
        }

        #endregion
    }

    internal sealed class PhysicalItemData
    {
        public float Mass { get; private set; }
        public float Volume { get; private set; }

        public PhysicalItemData(float mass, float volume)
        {
            Mass = mass;
            Volume = volume;
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
        public bool IsBuildMenuReachable { get; private set; }
        public string BlockPairName { get; private set; }
        public IReadOnlyList<MyDefinitionId> RelatedBlocks { get; private set; }
        public IReadOnlyList<BlockComponentRequirement> Components { get; private set; }

        public CubeBlockData(
            MyCubeSize cubeSize,
            Vector3I size,
            int pcu,
            bool isBuildMenuReachable,
            string blockPairName,
            IList<MyDefinitionId> relatedBlocks,
            IList<BlockComponentRequirement> components)
        {
            CubeSize = cubeSize;
            Size = size;
            Pcu = pcu;
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
        #region State

        private static readonly char[] LineSeparators =
            { '\r', '\n', '\u0085', '\u2028', '\u2029' };

        public MyDefinitionId Id { get; private set; }
        public string AuthoredDisplayName { get; private set; }
        public string UiDisplayName { get; private set; }
        public string Description { get; private set; }
        public string RuntimeTypeName { get; private set; }
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

        #endregion

        #region Construction

        public DefinitionDocument(
            MyDefinitionId id,
            string authoredDisplayName,
            string description,
            string runtimeTypeName,
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
            AuthoredDisplayName = authoredDisplayName ?? string.Empty;
            UiDisplayName = GetFirstAuthoredLine(AuthoredDisplayName, id);
            Description = description;
            RuntimeTypeName = runtimeTypeName;
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

        #endregion

        #region Display Names

        private static string GetFirstAuthoredLine(string displayName, MyDefinitionId id)
        {
            string[] lines = displayName.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();
                if (line.Length > 0)
                    return line;
            }

            return !string.IsNullOrWhiteSpace(id.SubtypeName)
                ? id.SubtypeName
                : id.ToString();
        }

        #endregion
    }
}
