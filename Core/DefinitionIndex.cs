using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Collections;
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
        public int SourceCount { get; private set; }
        public int SkippedCount { get; private set; }
        public int IssueCount { get; private set; }
        public DefinitionIconStats IconStats { get; private set; }

        private DefinitionIndex(
            IList<DefinitionDocument> definitions,
            IList<RecipeDocument> recipes,
            IDictionary<MyDefinitionId, List<BlockUsage>> mutableBlockUsage,
            int sourceCount,
            int skippedCount,
            int issueCount,
            DefinitionIconStats iconStats)
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
            SourceCount = sourceCount;
            SkippedCount = skippedCount;
            IssueCount = issueCount;
            IconStats = iconStats;
        }

        public static DefinitionIndex Build(MyDefinitionManager manager, bool survivalMode, Action<string> logWarning)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            var sourceDefinitions = new List<MyDefinitionBase>(manager.GetAllDefinitions());
            var sourceDefinitionIds = new HashSet<MyDefinitionId>();
            for (int definitionIndex = 0; definitionIndex < sourceDefinitions.Count; definitionIndex++)
            {
                MyDefinitionBase definition = sourceDefinitions[definitionIndex];
                if (definition != null)
                    sourceDefinitionIds.Add(definition.Id);
            }

            // Blueprint definitions live in their own registry and are not returned by
            // GetAllDefinitions(). Merge them before building documents and relations.
            foreach (MyBlueprintDefinitionBase blueprint in manager.GetBlueprintDefinitions())
            {
                if (blueprint != null && sourceDefinitionIds.Add(blueprint.Id))
                    sourceDefinitions.Add(blueprint);
            }

            HashSet<MyDefinitionId> buildMenuReachable = BuildMenuReachability(manager, sourceDefinitions, survivalMode, logWarning);
            Dictionary<MyDefinitionId, List<MyDefinitionId>> productionMenuReachability =
                BuildProductionMenuReachability(sourceDefinitions, survivalMode, logWarning);
            Dictionary<MyDefinitionId, List<MyDefinitionId>> blockRelationships = BuildBlockRelationships(manager, logWarning);
            var iconResolver = new DefinitionIconResolver(manager, logWarning);
            var documents = new List<DefinitionDocument>();
            var recipes = new List<RecipeDocument>();
            var blockUsage = new Dictionary<MyDefinitionId, List<BlockUsage>>();
            var ids = new HashSet<MyDefinitionId>();
            int sourceCount = 0;
            int skippedCount = 0;
            int issueCount = 0;

            foreach (MyDefinitionBase definition in sourceDefinitions)
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
                    PlanetGeneratorData planetGeneratorData = null;
                    AsteroidGeneratorData asteroidGeneratorData = null;
                    BrowseCategory browseCategory = BrowseCategory.None;

                    MyPhysicalItemDefinition physicalDefinition = definition as MyPhysicalItemDefinition;
                    if (physicalDefinition != null)
                    {
                        categories |= DefinitionCategory.PhysicalItem;
                        if (definition is MyComponentDefinition)
                            categories |= DefinitionCategory.Component;

                        TryExtractPhysical(physicalDefinition, ref categories, out physicalData, ref issueCount, logWarning);
                        browseCategory = GetPhysicalBrowseCategory(physicalDefinition);
                    }

                    MyBlueprintDefinitionBase blueprintDefinition = definition as MyBlueprintDefinitionBase;
                    if (blueprintDefinition != null)
                    {
                        categories |= DefinitionCategory.Blueprint;
                        List<MyDefinitionId> productionBlocks;
                        if (!productionMenuReachability.TryGetValue(blueprintDefinition.Id, out productionBlocks))
                            productionBlocks = new List<MyDefinitionId>();
                        recipeData = ExtractRecipe(blueprintDefinition, productionBlocks, ref issueCount, logWarning);
                        if (recipeData != null)
                        {
                            recipes.Add(recipeData);
                            if (recipeData.ProductionMenuReachable)
                                browseCategory = BrowseCategory.Recipes;
                        }
                    }

                    MyCubeBlockDefinition blockDefinition = definition as MyCubeBlockDefinition;
                    if (blockDefinition != null)
                    {
                        categories |= DefinitionCategory.CubeBlock;
                        browseCategory = BrowseCategory.Blocks;
                        blockData = ExtractBlock(
                            blockDefinition,
                            buildMenuReachable.Contains(blockDefinition.Id),
                            blockRelationships,
                            blockUsage,
                            ref issueCount,
                            logWarning);
                    }

                    MyPlanetGeneratorDefinition planetGenerator = definition as MyPlanetGeneratorDefinition;
                    if (planetGenerator != null)
                    {
                        browseCategory = BrowseCategory.Celestial;
                        planetGeneratorData = ExtractPlanetGenerator(planetGenerator, ref issueCount, logWarning);
                    }

                    MyAsteroidGeneratorDefinition asteroidGenerator = definition as MyAsteroidGeneratorDefinition;
                    if (asteroidGenerator != null)
                    {
                        browseCategory = BrowseCategory.Celestial;
                        asteroidGeneratorData = ExtractAsteroidGenerator(asteroidGenerator, ref issueCount, logWarning);
                    }

                    List<string> icons = GetIcons(definition);
                    documents.Add(new DefinitionDocument(
                        id,
                        GetDisplayName(definition, id),
                        GetDescription(definition),
                        definition.GetType().FullName ?? definition.GetType().Name,
                        iconResolver.Resolve(definition, icons),
                        categories,
                        browseCategory,
                        GetOrigin(definition, ref issueCount, logWarning),
                        definition.Enabled,
                        definition.Public,
                        definition.AvailableInSurvival,
                        physicalData,
                        recipeData,
                        blockData,
                        planetGeneratorData,
                        asteroidGeneratorData));
                }
                catch (Exception exception)
                {
                    skippedCount++;
                    issueCount++;
                    Warn(logWarning, "Skipped malformed definition: " + exception.Message);
                }
            }

            return new DefinitionIndex(
                documents,
                recipes,
                blockUsage,
                sourceCount,
                skippedCount,
                issueCount,
                iconResolver.GetStats());
        }

        private static Dictionary<MyDefinitionId, List<MyDefinitionId>> BuildProductionMenuReachability(
            IList<MyDefinitionBase> definitions,
            bool survivalMode,
            Action<string> logWarning)
        {
            var reachable = new Dictionary<MyDefinitionId, List<MyDefinitionId>>();

            for (int definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
            {
                MyProductionBlockDefinition block = definitions[definitionIndex] as MyProductionBlockDefinition;
                if (block == null)
                    continue;

                try
                {
                    if (!block.Enabled || !block.Public || (survivalMode && !block.AvailableInSurvival) ||
                        block.BlueprintClasses == null)
                        continue;

                    for (int classIndex = 0; classIndex < block.BlueprintClasses.Count; classIndex++)
                    {
                        MyBlueprintClassDefinition blueprintClass = block.BlueprintClasses[classIndex];
                        if (blueprintClass == null)
                            continue;

                        foreach (MyBlueprintDefinitionBase blueprint in blueprintClass)
                        {
                            // BlueprintClasses is the production block's postprocessed menu list.
                            // The vanilla production screen applies Public here, not the generic
                            // AvailableInSurvival flag inherited by every definition type.
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
                    Warn(logWarning, "Could not read production-menu recipes for " + block.Id + ": " + exception.Message);
                }
            }

            return reachable;
        }

        private static HashSet<MyDefinitionId> BuildMenuReachability(
            MyDefinitionManager manager,
            IList<MyDefinitionBase> definitions,
            bool survivalMode,
            Action<string> logWarning)
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
                        for (int variantIndex = 0; variantIndex < attachedGroup.Blocks.Length; variantIndex++)
                        {
                            if (attachedGroup.Blocks[variantIndex] != null)
                                reachable.Add(attachedGroup.Blocks[variantIndex].Id);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Warn(logWarning, "Could not read G-menu visibility for " + block.Id + ": " + exception.Message);
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
                        if (group == null || !group.Enabled || !group.Public || (survivalMode && !group.AvailableInSurvival))
                            continue;

                        MyCubeBlockDefinition[] blocks = group.Blocks;
                        if (blocks == null)
                            continue;

                        for (int index = 0; index < blocks.Length; index++)
                        {
                            try
                            {
                                if (blocks[index] != null)
                                    reachable.Add(blocks[index].Id);
                            }
                            catch (Exception exception)
                            {
                                Warn(logWarning, "Skipped malformed block variant in " + pair.Key + ": " + exception.Message);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Warn(logWarning, "Skipped malformed block variant group " + pair.Key + ": " + exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                Warn(logWarning, "Could not enumerate block variant groups: " + exception.Message);
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
                        if ((small != null && reachable.Contains(small.Id)) || (large != null && reachable.Contains(large.Id)))
                        {
                            if (small != null)
                                reachable.Add(small.Id);
                            if (large != null)
                                reachable.Add(large.Id);
                        }
                    }
                    catch (Exception exception)
                    {
                        Warn(logWarning, "Skipped malformed block pair " + pair.Key + ": " + exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                Warn(logWarning, "Could not enumerate block definition pairs: " + exception.Message);
            }

            return reachable;
        }

        private static BrowseCategory GetPhysicalBrowseCategory(MyPhysicalItemDefinition definition)
        {
            if (definition is MyComponentDefinition)
                return BrowseCategory.Components;
            if (definition.IsOre)
                return BrowseCategory.Ores;
            if (definition.IsIngot)
                return BrowseCategory.Ingots;
            if (definition is MyAmmoMagazineDefinition)
                return BrowseCategory.Ammo;
            if (definition is MyOxygenContainerDefinition)
                return BrowseCategory.GasBottles;
            if (definition is MyConsumableItemDefinition)
                return BrowseCategory.Consumables;
            if (definition is MyToolItemDefinition || definition is MyWeaponItemDefinition)
                return BrowseCategory.ToolsAndWeapons;
            return BrowseCategory.Items;
        }

        private static Dictionary<MyDefinitionId, List<MyDefinitionId>> BuildBlockRelationships(
            MyDefinitionManager manager,
            Action<string> logWarning)
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
                                    AddBlockRelationship(relationships, blocks[left].Id, blocks[right].Id);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Warn(logWarning, "Skipped malformed relationships in block variant group " + pair.Key + ": " + exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                Warn(logWarning, "Could not snapshot block variant relationships: " + exception.Message);
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
                            AddBlockRelationship(relationships, small.Id, large.Id);
                            AddBlockRelationship(relationships, large.Id, small.Id);
                        }
                    }
                    catch (Exception exception)
                    {
                        Warn(logWarning, "Skipped malformed relationships in block pair " + pair.Key + ": " + exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                Warn(logWarning, "Could not snapshot paired block relationships: " + exception.Message);
            }
            return relationships;
        }

        private static void AddBlockRelationship(
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
            IList<MyDefinitionId> productionBlocks,
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
                    results,
                    productionBlocks);
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
            bool buildMenuReachable,
            IDictionary<MyDefinitionId, List<MyDefinitionId>> blockRelationships,
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

                List<MyDefinitionId> relatedBlocks;
                if (!blockRelationships.TryGetValue(definition.Id, out relatedBlocks))
                    relatedBlocks = new List<MyDefinitionId>();

                return new CubeBlockData(
                    definition.CubeSize,
                    definition.Size,
                    definition.PCU,
                    definition.GuiVisible,
                    buildMenuReachable,
                    definition.BlockPairName,
                    relatedBlocks,
                    requirements);
            }
            catch (Exception exception)
            {
                issueCount++;
                Warn(logWarning, "Could not read cube block data for " + definition.Id + ": " + exception.Message);
                return null;
            }
        }

        private static PlanetGeneratorData ExtractPlanetGenerator(
            MyPlanetGeneratorDefinition definition,
            ref int issueCount,
            Action<string> logWarning)
        {
            try
            {
                var weatherTypes = new List<string>();
                if (definition.WeatherGenerators != null)
                {
                    for (int generatorIndex = 0; generatorIndex < definition.WeatherGenerators.Count; generatorIndex++)
                    {
                        try
                        {
                            MyWeatherGeneratorSettings generator = definition.WeatherGenerators[generatorIndex];
                            if (generator == null || generator.Weathers == null)
                                continue;
                            for (int weatherIndex = 0; weatherIndex < generator.Weathers.Count; weatherIndex++)
                            {
                                MyWeatherGeneratorVoxelSettings weather = generator.Weathers[weatherIndex];
                                if (weather != null && !string.IsNullOrWhiteSpace(weather.Name))
                                    weatherTypes.Add(weather.Name + " (weight " + weather.Weight + ")");
                            }
                        }
                        catch (Exception exception)
                        {
                            issueCount++;
                            Warn(logWarning, "Skipped weather entry in " + definition.Id + ": " + exception.Message);
                        }
                    }
                }

                var ores = new List<PlanetOreData>();
                if (definition.OreMappings != null)
                {
                    for (int index = 0; index < definition.OreMappings.Length; index++)
                    {
                        try
                        {
                            MyPlanetOreMapping ore = definition.OreMappings[index];
                            if (ore != null)
                                ores.Add(new PlanetOreData(ore.Type, ore.Start, ore.Depth));
                        }
                        catch (Exception exception)
                        {
                            issueCount++;
                            Warn(logWarning, "Skipped ore mapping in " + definition.Id + ": " + exception.Message);
                        }
                    }
                }

                MyPlanetAtmosphere atmosphere = definition.Atmosphere;
                return new PlanetGeneratorData(
                    definition.SurfaceGravity,
                    definition.GravityFalloffPower,
                    definition.HasAtmosphere,
                    definition.AtmosphereHeight,
                    atmosphere != null && atmosphere.Breathable,
                    atmosphere != null ? atmosphere.Density : 0f,
                    atmosphere != null ? atmosphere.OxygenDensity : 0f,
                    atmosphere != null ? atmosphere.LimitAltitude : 0f,
                    atmosphere != null ? atmosphere.MaxWindSpeed : 0f,
                    definition.DefaultSurfaceTemperature.ToString(),
                    definition.WeatherFrequencyMin,
                    definition.WeatherFrequencyMax,
                    definition.PersistentWeather,
                    weatherTypes,
                    ores);
            }
            catch (Exception exception)
            {
                issueCount++;
                Warn(logWarning, "Could not read planet generator data for " + definition.Id + ": " + exception.Message);
                return null;
            }
        }

        private static AsteroidGeneratorData ExtractAsteroidGenerator(
            MyAsteroidGeneratorDefinition definition,
            ref int issueCount,
            Action<string> logWarning)
        {
            try
            {
                var seedProbabilities = new List<string>();
                foreach (KeyValuePair<MyObjectSeedType, double> pair in definition.SeedTypeProbability)
                    seedProbabilities.Add(pair.Key + ": " + pair.Value.ToString("0.###"));

                var clusterProbabilities = new List<string>();
                foreach (KeyValuePair<MyObjectSeedType, double> pair in definition.SeedClusterTypeProbability)
                    clusterProbabilities.Add(pair.Key + ": " + pair.Value.ToString("0.###"));

                return new AsteroidGeneratorData(
                    definition.Version,
                    definition.ObjectSizeMin,
                    definition.ObjectSizeMax,
                    definition.ObjectSizeMinCluster,
                    definition.ObjectSizeMaxCluster,
                    definition.ObjectMaxInCluster,
                    definition.ObjectMinDistanceInCluster,
                    definition.ObjectMaxDistanceInClusterMin,
                    definition.ObjectMaxDistanceInClusterMax,
                    definition.ObjectDensityCluster,
                    definition.ClusterDispersionAbsolute,
                    definition.RotateAsteroids,
                    definition.UseClusterVariableSize,
                    seedProbabilities,
                    clusterProbabilities);
            }
            catch (Exception exception)
            {
                issueCount++;
                Warn(logWarning, "Could not read asteroid generator data for " + definition.Id + ": " + exception.Message);
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

        private static List<string> GetIcons(MyDefinitionBase definition)
        {
            var icons = new List<string>();
            try
            {
                if (definition.Icons == null)
                    return icons;

                for (int index = 0; index < definition.Icons.Length; index++)
                {
                    string icon = definition.Icons[index];
                    if (!string.IsNullOrWhiteSpace(icon))
                        icons.Add(icon);
                }
            }
            catch
            { }
            return icons;
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
