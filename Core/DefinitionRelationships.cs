using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Collections;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class DefinitionRelationships
    {
        private static readonly IReadOnlyList<MyDefinitionId> EmptyIds =
            new List<MyDefinitionId>().AsReadOnly();

        private readonly HashSet<MyDefinitionId> buildMenuDefinitions;
        private readonly Dictionary<MyDefinitionId, List<MyDefinitionId>> productionBlocksByRecipe;
        private readonly Dictionary<MyDefinitionId, List<MyDefinitionId>> relatedBlocks;

        private DefinitionRelationships(
            HashSet<MyDefinitionId> buildMenuDefinitions,
            Dictionary<MyDefinitionId, List<MyDefinitionId>> productionBlocksByRecipe,
            Dictionary<MyDefinitionId, List<MyDefinitionId>> relatedBlocks)
        {
            this.buildMenuDefinitions = buildMenuDefinitions;
            this.productionBlocksByRecipe = productionBlocksByRecipe;
            this.relatedBlocks = relatedBlocks;
        }

        public static DefinitionRelationships Build(
            MyDefinitionManager manager,
            IList<MyDefinitionBase> definitions,
            bool survivalMode,
            DefinitionBuildDiagnostics diagnostics)
        {
            return new DefinitionRelationships(
                BuildMenuReachability(manager, definitions, survivalMode, diagnostics),
                BuildProductionMenuReachability(definitions, survivalMode, diagnostics),
                BuildBlockRelationships(manager, diagnostics));
        }

        public bool IsBuildMenuReachable(MyDefinitionId id)
        {
            return buildMenuDefinitions.Contains(id);
        }

        public IReadOnlyList<MyDefinitionId> GetProductionBlocks(MyDefinitionId recipeId)
        {
            List<MyDefinitionId> blocks;
            return productionBlocksByRecipe.TryGetValue(recipeId, out blocks) ? blocks : EmptyIds;
        }

        public IReadOnlyList<MyDefinitionId> GetRelatedBlocks(MyDefinitionId blockId)
        {
            List<MyDefinitionId> blocks;
            return relatedBlocks.TryGetValue(blockId, out blocks) ? blocks : EmptyIds;
        }

        private static Dictionary<MyDefinitionId, List<MyDefinitionId>> BuildProductionMenuReachability(
            IList<MyDefinitionBase> definitions,
            bool survivalMode,
            DefinitionBuildDiagnostics diagnostics)
        {
            var reachable = new Dictionary<MyDefinitionId, List<MyDefinitionId>>();

            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                MyProductionBlockDefinition block = definitions[definitionIndex] as MyProductionBlockDefinition;
                if (block == null)
                    continue;

                try
                {
                    if (!block.Enabled || !block.Public ||
                        (survivalMode && !block.AvailableInSurvival) || block.BlueprintClasses == null)
                        continue;

                    for (int classIndex = 0; classIndex < block.BlueprintClasses.Count; classIndex++)
                    {
                        MyBlueprintClassDefinition blueprintClass = block.BlueprintClasses[classIndex];
                        if (blueprintClass == null)
                            continue;

                        foreach (MyBlueprintDefinitionBase blueprint in blueprintClass)
                        {
                            // BlueprintClasses is the production block's postprocessed menu list.
                            // Vanilla checks Public here rather than the inherited survival flag.
                            if (blueprint == null || !blueprint.Public)
                                continue;

                            List<MyDefinitionId> blocks;
                            if (!reachable.TryGetValue(blueprint.Id, out blocks))
                            {
                                blocks = new List<MyDefinitionId>();
                                reachable.Add(blueprint.Id, blocks);
                            }

                            if (!blocks.Contains(block.Id))
                                blocks.Add(block.Id);
                        }
                    }
                }
                catch (Exception exception)
                {
                    diagnostics.Report(
                        "production-menu",
                        "Could not read recipes for " + block.Id,
                        exception);
                }
            }

            return reachable;
        }

        private static HashSet<MyDefinitionId> BuildMenuReachability(
            MyDefinitionManager manager,
            IList<MyDefinitionBase> definitions,
            bool survivalMode,
            DefinitionBuildDiagnostics diagnostics)
        {
            var reachable = new HashSet<MyDefinitionId>();

            for (int index = 0; index < definitions.Count; index++)
            {
                MyCubeBlockDefinition block = definitions[index] as MyCubeBlockDefinition;
                if (block == null)
                    continue;

                try
                {
                    if (block.GuiVisible)
                        reachable.Add(block.Id);

                    MyBlockVariantGroup attachedGroup = block.BlockVariantsGroup;
                    if (attachedGroup != null && attachedGroup.Enabled && attachedGroup.Public &&
                        (!survivalMode || attachedGroup.AvailableInSurvival) && attachedGroup.Blocks != null)
                    {
                        AddBlocks(reachable, attachedGroup.Blocks);
                    }
                }
                catch (Exception exception)
                {
                    diagnostics.Report(
                        "build-menu-block",
                        "Could not read visibility for " + block.Id,
                        exception);
                }
            }

            try
            {
                DictionaryReader<string, MyBlockVariantGroup> groups = manager.GetBlockVariantGroupDefinitions();
                foreach (KeyValuePair<string, MyBlockVariantGroup> pair in groups)
                {
                    try
                    {
                        MyBlockVariantGroup group = pair.Value;
                        if (group == null || !group.Enabled || !group.Public ||
                            (survivalMode && !group.AvailableInSurvival) || group.Blocks == null)
                            continue;

                        AddBlocks(reachable, group.Blocks);
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Report(
                            "build-menu-variant",
                            "Skipped variant group " + pair.Key,
                            exception);
                    }
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report("build-menu-variants", "Could not enumerate variant groups", exception);
            }

            try
            {
                DictionaryReader<string, MyCubeBlockDefinitionGroup> pairs = manager.GetDefinitionPairs();
                foreach (KeyValuePair<string, MyCubeBlockDefinitionGroup> pair in pairs)
                {
                    try
                    {
                        MyCubeBlockDefinition small = pair.Value.Small;
                        MyCubeBlockDefinition large = pair.Value.Large;
                        if ((small != null && reachable.Contains(small.Id)) ||
                            (large != null && reachable.Contains(large.Id)))
                        {
                            if (small != null)
                                reachable.Add(small.Id);
                            if (large != null)
                                reachable.Add(large.Id);
                        }
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Report(
                            "build-menu-pair",
                            "Skipped block pair " + pair.Key,
                            exception);
                    }
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report("build-menu-pairs", "Could not enumerate block pairs", exception);
            }

            return reachable;
        }

        private static Dictionary<MyDefinitionId, List<MyDefinitionId>> BuildBlockRelationships(
            MyDefinitionManager manager,
            DefinitionBuildDiagnostics diagnostics)
        {
            var relationships = new Dictionary<MyDefinitionId, List<MyDefinitionId>>();

            try
            {
                foreach (KeyValuePair<string, MyBlockVariantGroup> pair in manager.GetBlockVariantGroupDefinitions())
                {
                    try
                    {
                        MyCubeBlockDefinition[] blocks = pair.Value != null ? pair.Value.Blocks : null;
                        if (blocks == null)
                            continue;

                        for (int left = 0; left < blocks.Length; left++)
                        {
                            if (blocks[left] == null)
                                continue;
                            for (int right = 0; right < blocks.Length; right++)
                            {
                                if (left != right && blocks[right] != null)
                                    AddRelationship(relationships, blocks[left].Id, blocks[right].Id);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Report(
                            "block-relationship-variant",
                            "Skipped variant group " + pair.Key,
                            exception);
                    }
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "block-relationships-variants",
                    "Could not enumerate variant relationships",
                    exception);
            }

            try
            {
                foreach (KeyValuePair<string, MyCubeBlockDefinitionGroup> pair in manager.GetDefinitionPairs())
                {
                    try
                    {
                        MyCubeBlockDefinition small = pair.Value.Small;
                        MyCubeBlockDefinition large = pair.Value.Large;
                        if (small != null && large != null)
                        {
                            AddRelationship(relationships, small.Id, large.Id);
                            AddRelationship(relationships, large.Id, small.Id);
                        }
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Report(
                            "block-relationship-pair",
                            "Skipped block pair " + pair.Key,
                            exception);
                    }
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "block-relationships-pairs",
                    "Could not enumerate paired relationships",
                    exception);
            }

            return relationships;
        }

        private static void AddBlocks(HashSet<MyDefinitionId> reachable, MyCubeBlockDefinition[] blocks)
        {
            for (int index = 0; index < blocks.Length; index++)
            {
                if (blocks[index] != null)
                    reachable.Add(blocks[index].Id);
            }
        }

        private static void AddRelationship(
            IDictionary<MyDefinitionId, List<MyDefinitionId>> relationships,
            MyDefinitionId source,
            MyDefinitionId target)
        {
            List<MyDefinitionId> related;
            if (!relationships.TryGetValue(source, out related))
            {
                related = new List<MyDefinitionId>();
                relationships.Add(source, related);
            }
            if (!related.Contains(target))
                related.Add(target);
        }
    }
}
