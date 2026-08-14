using System.Collections.Generic;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class DefinitionIndex
    {
        private static readonly IReadOnlyList<BlockUsage> EmptyBlockUsages =
            new List<BlockUsage>().AsReadOnly();

        private readonly Dictionary<MyDefinitionId, DefinitionDocument> byId;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<BlockUsage>> blocksUsingItem;

        public IReadOnlyList<DefinitionDocument> All { get; private set; }
        public RecipeIndex Recipes { get; private set; }
        public int SourceCount { get; private set; }
        public int IssueCount { get; private set; }
        public DefinitionIconStats IconStats { get; private set; }

        public DefinitionIndex(
            IList<DefinitionDocument> definitions,
            int sourceCount,
            int issueCount,
            DefinitionIconStats iconStats)
        {
            var sorted = new List<DefinitionDocument>(definitions);
            sorted.Sort(CompareDefinitions);
            All = sorted.AsReadOnly();

            byId = new Dictionary<MyDefinitionId, DefinitionDocument>();
            var recipes = new List<RecipeDocument>();
            var mutableBlockUsage = new Dictionary<MyDefinitionId, List<BlockUsage>>();

            for (int index = 0; index < sorted.Count; index++)
            {
                DefinitionDocument definition = sorted[index];
                byId.Add(definition.Id, definition);

                if (definition.Recipe != null)
                    recipes.Add(definition.Recipe);
                if (definition.CubeBlock != null)
                    AddBlockUsage(mutableBlockUsage, definition);
            }

            blocksUsingItem = new Dictionary<MyDefinitionId, IReadOnlyList<BlockUsage>>();
            foreach (KeyValuePair<MyDefinitionId, List<BlockUsage>> pair in mutableBlockUsage)
            {
                pair.Value.Sort(CompareBlockUsages);
                blocksUsingItem.Add(pair.Key, pair.Value.AsReadOnly());
            }

            Recipes = new RecipeIndex(recipes);
            SourceCount = sourceCount;
            IssueCount = issueCount;
            IconStats = iconStats;
        }

        public bool TryGet(MyDefinitionId id, out DefinitionDocument definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        public IReadOnlyList<BlockUsage> GetBlocksUsing(MyDefinitionId itemId)
        {
            IReadOnlyList<BlockUsage> usages;
            return blocksUsingItem.TryGetValue(itemId, out usages) ? usages : EmptyBlockUsages;
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

        private int CompareBlockUsages(BlockUsage left, BlockUsage right)
        {
            DefinitionDocument leftDefinition;
            DefinitionDocument rightDefinition;
            string leftName = byId.TryGetValue(left.BlockId, out leftDefinition)
                ? leftDefinition.DisplayName
                : left.BlockId.ToString();
            string rightName = byId.TryGetValue(right.BlockId, out rightDefinition)
                ? rightDefinition.DisplayName
                : right.BlockId.ToString();
            return string.Compare(leftName, rightName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareDefinitions(DefinitionDocument left, DefinitionDocument right)
        {
            int name = string.Compare(
                left.DisplayName,
                right.DisplayName,
                System.StringComparison.OrdinalIgnoreCase);
            return name != 0
                ? name
                : string.Compare(
                    left.Id.ToString(),
                    right.Id.ToString(),
                    System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
