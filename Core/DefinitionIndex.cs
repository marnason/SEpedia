using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;

namespace SEpedia.Core
{
    public sealed class DefinitionIndex
    {
        private static readonly IReadOnlyList<BlockUsage> EmptyBlockUsages = new List<BlockUsage>().AsReadOnly();

        private readonly Dictionary<MyDefinitionId, DefinitionDocument> byId;
        private readonly Dictionary<MyDefinitionId, IReadOnlyList<BlockUsage>> blocksUsingItem;

        public IReadOnlyList<DefinitionDocument> All { get; private set; }
        public RecipeIndex Recipes { get; private set; }
        public SearchIndex Search { get; private set; }
        public int SourceCount { get; private set; }
        public int SkippedCount { get; private set; }
        public int IssueCount { get; private set; }

        private DefinitionIndex(
            IList<DefinitionDocument> definitions,
            IList<RecipeDocument> recipes,
            IDictionary<MyDefinitionId, List<BlockUsage>> mutableBlockUsage,
            int sourceCount,
            int skippedCount,
            int issueCount)
        {
            var sorted = new List<DefinitionDocument>(definitions);
            sorted.Sort(CompareDefinitions);
            All = sorted.AsReadOnly();

            byId = new Dictionary<MyDefinitionId, DefinitionDocument>();
            for (int index = 0; index < sorted.Count; index++)
                byId[sorted[index].Id] = sorted[index];

            blocksUsingItem = new Dictionary<MyDefinitionId, IReadOnlyList<BlockUsage>>();
            foreach (KeyValuePair<MyDefinitionId, List<BlockUsage>> pair in mutableBlockUsage)
                blocksUsingItem.Add(pair.Key, pair.Value.AsReadOnly());

            Recipes = new RecipeIndex(recipes);
            Search = new SearchIndex(sorted);
            SourceCount = sourceCount;
            SkippedCount = skippedCount;
            IssueCount = issueCount;
        }

        public static DefinitionIndex Build(IEnumerable<MyDefinitionBase> definitions, Action<string> logWarning)
        {
            var documents = new List<DefinitionDocument>();
            var recipes = new List<RecipeDocument>();
            var blockUsage = new Dictionary<MyDefinitionId, List<BlockUsage>>();
            var ids = new HashSet<MyDefinitionId>();
            int sourceCount = 0;
            int skippedCount = 0;
            int issueCount = 0;

            foreach (MyDefinitionBase definition in definitions)
            {
                sourceCount++;

                if (definition == null)
                {
                    skippedCount++;
                    issueCount++;
                    Warn(logWarning, "Skipped a null definition in the runtime registry.");
                    continue;
                }

                try
                {
                    MyDefinitionId id = definition.Id;
                    if (!ids.Add(id))
                    {
                        skippedCount++;
                        issueCount++;
                        Warn(logWarning, "Skipped duplicate definition ID " + id + ".");
                        continue;
                    }

                    DefinitionCategory categories = DefinitionCategory.None;
                    PhysicalItemData physicalData = null;
                    RecipeDocument recipeData = null;
                    CubeBlockData blockData = null;

                    MyPhysicalItemDefinition physicalDefinition = definition as MyPhysicalItemDefinition;
                    if (physicalDefinition != null)
                    {
                        categories |= DefinitionCategory.PhysicalItem;
                        if (definition is MyComponentDefinition)
                            categories |= DefinitionCategory.Component;

                        TryExtractPhysical(physicalDefinition, ref categories, out physicalData, ref issueCount, logWarning);
                    }

                    MyBlueprintDefinitionBase blueprintDefinition = definition as MyBlueprintDefinitionBase;
                    if (blueprintDefinition != null)
                    {
                        categories |= DefinitionCategory.Blueprint;
                        recipeData = ExtractRecipe(blueprintDefinition, ref issueCount, logWarning);
                        if (recipeData != null)
                            recipes.Add(recipeData);
                    }

                    MyCubeBlockDefinition blockDefinition = definition as MyCubeBlockDefinition;
                    if (blockDefinition != null)
                    {
                        categories |= DefinitionCategory.CubeBlock;
                        blockData = ExtractBlock(blockDefinition, blockUsage, ref issueCount, logWarning);
                    }

                    documents.Add(new DefinitionDocument(
                        id,
                        GetDisplayName(definition, id),
                        GetDescription(definition),
                        definition.GetType().FullName ?? definition.GetType().Name,
                        categories,
                        GetOrigin(definition, ref issueCount, logWarning),
                        definition.Enabled,
                        definition.Public,
                        definition.AvailableInSurvival,
                        physicalData,
                        recipeData,
                        blockData));
                }
                catch (Exception exception)
                {
                    skippedCount++;
                    issueCount++;
                    Warn(logWarning, "Skipped malformed definition: " + exception.Message);
                }
            }

            return new DefinitionIndex(documents, recipes, blockUsage, sourceCount, skippedCount, issueCount);
        }

        public bool TryGet(MyDefinitionId id, out DefinitionDocument definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        public IReadOnlyList<BlockUsage> GetBlocksUsing(MyDefinitionId componentId)
        {
            IReadOnlyList<BlockUsage> usage;
            return blocksUsingItem.TryGetValue(componentId, out usage) ? usage : EmptyBlockUsages;
        }

        private static void TryExtractPhysical(
            MyPhysicalItemDefinition definition,
            ref DefinitionCategory categories,
            out PhysicalItemData data,
            ref int issueCount,
            Action<string> logWarning)
        {
            data = null;
            try
            {
                if (definition.IsOre)
                    categories |= DefinitionCategory.Ore;
                if (definition.IsIngot)
                    categories |= DefinitionCategory.Ingot;

                data = new PhysicalItemData(
                    definition.Mass,
                    definition.Volume,
                    definition.MaxStackAmount,
                    definition.HasIntegralAmounts);
            }
            catch (Exception exception)
            {
                issueCount++;
                Warn(logWarning, "Could not read physical item data for " + definition.Id + ": " + exception.Message);
            }
        }

        private static RecipeDocument ExtractRecipe(
            MyBlueprintDefinitionBase definition,
            ref int issueCount,
            Action<string> logWarning)
        {
            try
            {
                List<DefinitionAmount> prerequisites = ExtractRecipeItems(
                    definition.Id,
                    "prerequisite",
                    definition.Prerequisites,
                    ref issueCount,
                    logWarning);
                List<DefinitionAmount> results = ExtractRecipeItems(
                    definition.Id,
                    "result",
                    definition.Results,
                    ref issueCount,
                    logWarning);

                return new RecipeDocument(
                    definition.Id,
                    definition.BaseProductionTimeInSeconds,
                    definition.Atomic,
                    prerequisites,
                    results);
            }
            catch (Exception exception)
            {
                issueCount++;
                Warn(logWarning, "Could not read recipe data for " + definition.Id + ": " + exception.Message);
                return null;
            }
        }

        private static List<DefinitionAmount> ExtractRecipeItems(
            MyDefinitionId recipeId,
            string relationshipName,
            MyBlueprintDefinitionBase.Item[] items,
            ref int issueCount,
            Action<string> logWarning)
        {
            var result = new List<DefinitionAmount>();
            if (items == null)
                return result;

            for (int index = 0; index < items.Length; index++)
            {
                try
                {
                    result.Add(new DefinitionAmount(items[index].Id, items[index].Amount));
                }
                catch (Exception exception)
                {
                    issueCount++;
                    Warn(logWarning, "Skipped recipe " + relationshipName + " in " + recipeId + ": " + exception.Message);
                }
            }

            return result;
        }

        private static CubeBlockData ExtractBlock(
            MyCubeBlockDefinition definition,
            IDictionary<MyDefinitionId, List<BlockUsage>> blockUsage,
            ref int issueCount,
            Action<string> logWarning)
        {
            var requirements = new List<BlockComponentRequirement>();

            try
            {
                MyCubeBlockDefinition.Component[] components = definition.Components;
                if (components != null)
                {
                    for (int index = 0; index < components.Length; index++)
                    {
                        try
                        {
                            MyCubeBlockDefinition.Component component = components[index];
                            if (component == null || component.Definition == null)
                                throw new InvalidOperationException("Component definition is missing.");

                            MyDefinitionId componentId = component.Definition.Id;
                            requirements.Add(new BlockComponentRequirement(componentId, component.Count));

                            List<BlockUsage> usages;
                            if (!blockUsage.TryGetValue(componentId, out usages))
                            {
                                usages = new List<BlockUsage>();
                                blockUsage.Add(componentId, usages);
                            }

                            usages.Add(new BlockUsage(definition.Id, component.Count));
                        }
                        catch (Exception exception)
                        {
                            issueCount++;
                            Warn(logWarning, "Skipped block component in " + definition.Id + ": " + exception.Message);
                        }
                    }
                }

                return new CubeBlockData(definition.CubeSize, definition.Size, definition.PCU, requirements);
            }
            catch (Exception exception)
            {
                issueCount++;
                Warn(logWarning, "Could not read cube block data for " + definition.Id + ": " + exception.Message);
                return null;
            }
        }

        private static DefinitionOrigin GetOrigin(MyDefinitionBase definition, ref int issueCount, Action<string> logWarning)
        {
            try
            {
                MyModContext context = definition.Context;
                if (context == null)
                    return DefinitionOrigin.Unknown;

                return new DefinitionOrigin(
                    context.IsBaseGame,
                    context.ModName,
                    context.ModId,
                    context.ModServiceName,
                    context.CurrentFile);
            }
            catch (Exception exception)
            {
                issueCount++;
                Warn(logWarning, "Could not read origin for " + definition.Id + ": " + exception.Message);
                return DefinitionOrigin.Unknown;
            }
        }

        private static string GetDisplayName(MyDefinitionBase definition, MyDefinitionId id)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(definition.DisplayNameText))
                    return definition.DisplayNameText;
            }
            catch
            { }

            try
            {
                if (!string.IsNullOrWhiteSpace(definition.DisplayNameString))
                    return definition.DisplayNameString;
            }
            catch
            { }

            return !string.IsNullOrWhiteSpace(id.SubtypeName) ? id.SubtypeName : id.ToString();
        }

        private static string GetDescription(MyDefinitionBase definition)
        {
            try
            {
                return definition.DescriptionText ?? string.Empty;
            }
            catch
            {
                try
                {
                    return definition.DescriptionString ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        private static int CompareDefinitions(DefinitionDocument left, DefinitionDocument right)
        {
            int nameComparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            return nameComparison != 0
                ? nameComparison
                : string.Compare(left.Id.ToString(), right.Id.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static void Warn(Action<string> logWarning, string message)
        {
            if (logWarning != null)
                logWarning(message);
        }
    }
}
