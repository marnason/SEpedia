using System.Collections.Generic;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class DefinitionIndex
    {
        #region State

        private static readonly IReadOnlyList<BlockUsage> EmptyBlockUsages =
            new List<BlockUsage>().AsReadOnly();
        private static readonly IReadOnlyList<PlanetOreUsage> EmptyPlanetOreUsages =
            new List<PlanetOreUsage>().AsReadOnly();

        private readonly Dictionary<MyDefinitionId, DefinitionDocument> byId;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<BlockUsage>> blocksUsingItem;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<PlanetOreUsage>> planetGeneratorsUsingOre;

        public IReadOnlyList<DefinitionDocument> All { get; private set; }
        public RecipeIndex Recipes { get; private set; }
        public int SourceCount { get; private set; }
        public int IssueCount { get; private set; }
        public int PlanetGeneratorCount { get; private set; }
        public int AsteroidGeneratorCount { get; private set; }

        #endregion

        #region Index Construction

        public DefinitionIndex(
            IList<DefinitionDocument> definitions,
            int sourceCount,
            int issueCount,
            int planetGeneratorCount,
            int asteroidGeneratorCount)
        {
            var sorted = new List<DefinitionDocument>(definitions);
            sorted.Sort(CompareDefinitions);
            All = sorted.AsReadOnly();

            byId = new Dictionary<MyDefinitionId, DefinitionDocument>();
            var recipes = new List<RecipeDocument>();
            var mutableBlockUsage = new Dictionary<MyDefinitionId, List<BlockUsage>>();
            var mutablePlanetOreUsage = new Dictionary<MyDefinitionId, List<PlanetOreUsage>>();

            for (int index = 0; index < sorted.Count; index++)
            {
                DefinitionDocument definition = sorted[index];
                byId.Add(definition.Id, definition);

                if (definition.Recipe != null)
                    recipes.Add(definition.Recipe);
                if (definition.CubeBlock != null)
                    AddBlockUsage(mutableBlockUsage, definition);
                if (definition.PlanetGenerator != null)
                    AddPlanetOreUsage(mutablePlanetOreUsage, definition);
            }

            blocksUsingItem = new Dictionary<MyDefinitionId, IReadOnlyList<BlockUsage>>();
            foreach (KeyValuePair<MyDefinitionId, List<BlockUsage>> pair in mutableBlockUsage)
            {
                pair.Value.Sort(CompareBlockUsages);
                blocksUsingItem.Add(pair.Key, pair.Value.AsReadOnly());
            }

            planetGeneratorsUsingOre = new Dictionary<MyDefinitionId, IReadOnlyList<PlanetOreUsage>>();
            foreach (KeyValuePair<MyDefinitionId, List<PlanetOreUsage>> pair in mutablePlanetOreUsage)
            {
                pair.Value.Sort(ComparePlanetOreUsages);
                planetGeneratorsUsingOre.Add(pair.Key, pair.Value.AsReadOnly());
            }

            Recipes = new RecipeIndex(recipes);
            SourceCount = sourceCount;
            IssueCount = issueCount;
            PlanetGeneratorCount = planetGeneratorCount;
            AsteroidGeneratorCount = asteroidGeneratorCount;
        }

        private static void AddBlockUsage(
            IDictionary<MyDefinitionId, List<BlockUsage>> target,
            DefinitionDocument block)
        {
            IReadOnlyList<BlockComponentRequirement> components = block.CubeBlock.Components;
            for (int index = 0; index < components.Count; index++)
            {
                BlockComponentRequirement component = components[index];
                List<BlockUsage> usages;
                if (!target.TryGetValue(component.ComponentId, out usages))
                {
                    usages = new List<BlockUsage>();
                    target.Add(component.ComponentId, usages);
                }
                usages.Add(new BlockUsage(block.Id, component.Count));
            }
        }

        private static void AddPlanetOreUsage(
            IDictionary<MyDefinitionId, List<PlanetOreUsage>> target,
            DefinitionDocument generator)
        {
            IReadOnlyList<PlanetOreData> mappings = generator.PlanetGenerator.Ores;
            for (int index = 0; index < mappings.Count; index++)
            {
                PlanetOreData mapping = mappings[index];
                if (!mapping.OreId.HasValue)
                    continue;

                List<PlanetOreUsage> usages;
                if (!target.TryGetValue(mapping.OreId.Value, out usages))
                {
                    usages = new List<PlanetOreUsage>();
                    target.Add(mapping.OreId.Value, usages);
                }
                usages.Add(new PlanetOreUsage(generator.Id, mapping));
            }
        }

        #endregion

        #region Queries

        public bool TryGet(MyDefinitionId id, out DefinitionDocument definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        public IReadOnlyList<BlockUsage> GetBlocksUsing(MyDefinitionId itemId)
        {
            IReadOnlyList<BlockUsage> usages;
            return blocksUsingItem.TryGetValue(itemId, out usages) ? usages : EmptyBlockUsages;
        }

        public IReadOnlyList<PlanetOreUsage> GetPlanetGeneratorsUsingOre(MyDefinitionId oreId)
        {
            IReadOnlyList<PlanetOreUsage> usages;
            return planetGeneratorsUsingOre.TryGetValue(oreId, out usages)
                ? usages
                : EmptyPlanetOreUsages;
        }

        #endregion

        #region Ordering

        private int CompareBlockUsages(BlockUsage left, BlockUsage right)
        {
            DefinitionDocument leftDefinition;
            DefinitionDocument rightDefinition;
            string leftName = byId.TryGetValue(left.BlockId, out leftDefinition)
                ? leftDefinition.UiDisplayName
                : left.BlockId.ToString();
            string rightName = byId.TryGetValue(right.BlockId, out rightDefinition)
                ? rightDefinition.UiDisplayName
                : right.BlockId.ToString();
            return string.Compare(leftName, rightName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareDefinitions(DefinitionDocument left, DefinitionDocument right)
        {
            int name = string.Compare(
                left.UiDisplayName,
                right.UiDisplayName,
                System.StringComparison.OrdinalIgnoreCase);
            return name != 0
                ? name
                : string.Compare(
                    left.Id.ToString(),
                    right.Id.ToString(),
                    System.StringComparison.OrdinalIgnoreCase);
        }

        private int ComparePlanetOreUsages(PlanetOreUsage left, PlanetOreUsage right)
        {
            DefinitionDocument leftGenerator;
            DefinitionDocument rightGenerator;
            string leftName = byId.TryGetValue(left.GeneratorId, out leftGenerator)
                ? leftGenerator.UiDisplayName
                : left.GeneratorId.ToString();
            string rightName = byId.TryGetValue(right.GeneratorId, out rightGenerator)
                ? rightGenerator.UiDisplayName
                : right.GeneratorId.ToString();
            int generator = string.Compare(leftName, rightName, System.StringComparison.OrdinalIgnoreCase);
            if (generator != 0)
                return generator;

            int material = string.Compare(
                left.Mapping.Material,
                right.Mapping.Material,
                System.StringComparison.OrdinalIgnoreCase);
            return material != 0
                ? material
                : left.Mapping.Start.CompareTo(right.Mapping.Start);
        }

        #endregion
    }
}
