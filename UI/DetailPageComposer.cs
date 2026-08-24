using System;
using System.Collections.Generic;
using System.Text;
using SEpedia.Core;
using VRage;
using VRage.Game;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DetailPageComposer
    {
        #region State and Construction

        private readonly DefinitionIndex index;
        private readonly CelestialIndex celestial;
        private readonly CatalogFilter filter;
        private readonly CatalogEntryVisibility visibility;
        private readonly DetailProviderRegistry providers;

        public DetailPageComposer(DefinitionIndex index, CelestialIndex celestial, CatalogFilter filter)
        {
            this.index = index;
            this.celestial = celestial;
            this.filter = filter;
            visibility = filter.EntryVisibility;
            providers = new DetailProviderRegistry();
            RegisterProviders();
        }

        #endregion

        #region Page Composition

        private void RegisterProviders()
        {
            providers.Register(new DelegateDetailProvider(10,
                delegate(CatalogEntry entry) { return entry.Definition != null; },
                delegate(DetailCompositionContext context) { AddDefinitionMetadata(context.Rows, context.Entry.Definition); }));
            providers.Register(new DelegateDetailProvider(20,
                delegate(CatalogEntry entry) { return entry.Definition != null && entry.Definition.PhysicalItem != null; },
                delegate(DetailCompositionContext context)
                {
                    DefinitionDocument definition = context.Entry.Definition;
                    AddHeading(context.Rows, "Physical item");
                    AddField(context.Rows, "Mass", definition.PhysicalItem.Mass.ToString("0.###"));
                    AddField(context.Rows, "Volume", definition.PhysicalItem.Volume.ToString("0.######"));
                }));
            providers.Register(new DelegateDetailProvider(30,
                delegate(CatalogEntry entry) { return entry.Definition != null && entry.Definition.Recipe != null; },
                delegate(DetailCompositionContext context) { AddRecipe(context.Rows, context.Entry.Definition.Recipe); }));
            providers.Register(new DelegateDetailProvider(40,
                delegate(CatalogEntry entry) { return entry.Definition != null && entry.Definition.CubeBlock != null; },
                delegate(DetailCompositionContext context) { AddBlock(context.Rows, context.Entry.Definition.CubeBlock); }));
            providers.Register(new DelegateDetailProvider(50,
                delegate(CatalogEntry entry) { return entry.Definition != null && entry.Definition.PlanetGenerator != null; },
                delegate(DetailCompositionContext context)
                {
                    AddPlanetGenerator(context.Rows, context.Entry.Definition.Id, context.Entry.Definition.PlanetGenerator);
                }));
            providers.Register(new DelegateDetailProvider(60,
                delegate(CatalogEntry entry) { return entry.Definition != null && entry.Definition.AsteroidGenerator != null; },
                delegate(DetailCompositionContext context) { AddAsteroidGenerator(context.Rows, context.Entry.Definition.AsteroidGenerator); }));
            providers.Register(new DelegateDetailProvider(70,
                delegate(CatalogEntry entry) { return entry.Definition != null; },
                delegate(DetailCompositionContext context) { AddReverseRelationships(context.Rows, context.Entry.Definition.Id); }));
            providers.Register(new DelegateDetailProvider(10,
                delegate(CatalogEntry entry) { return entry.Planet != null; },
                delegate(DetailCompositionContext context) { AddSpawnedPlanet(context.Rows, context.Entry.Planet); }));
        }

        public DetailPageModel Compose(CatalogEntry entry)
        {
            var rows = new List<DetailRowModel>();
            providers.Compose(entry, rows);
            if (entry.Definition != null)
            {
                DefinitionDocument definition = entry.Definition;
                return new DetailPageModel(definition.UiDisplayName, definition.Id.ToString(),
                    definition.RuntimeTypeName, definition.Description, rows);
            }
            PlanetSnapshot planet = entry.Planet;
            return new DetailPageModel(planet.DisplayName, planet.EntityId.ToString(),
                "Spawned planet", string.Empty, rows);
        }

        private static void AddDefinitionMetadata(IList<DetailRowModel> rows, DefinitionDocument definition)
        {
            AddField(rows, "Origin", definition.Origin.DisplayName);
            AddField(rows, "Flags", "Enabled: " + YesNo(definition.IsEnabled)
                + "   Public: " + YesNo(definition.IsPublic)
                + "   Survival: " + YesNo(definition.IsAvailableInSurvival));
        }

        private void AddSpawnedPlanet(IList<DetailRowModel> rows, PlanetSnapshot planet)
        {
            AddField(rows, "Position", FormatVector(planet.Position));
            AddField(rows, "Minimum radius", FormatDistance(planet.MinimumRadius));
            AddField(rows, "Average radius", FormatDistance(planet.AverageRadius));
            AddField(rows, "Maximum radius", FormatDistance(planet.MaximumRadius));
            AddField(rows, "Has atmosphere", YesNo(planet.HasAtmosphere));
            AddField(rows, "Atmosphere radius", FormatDistance(planet.AtmosphereRadius));
            AddField(rows, "Atmosphere altitude", FormatDistance(planet.AtmosphereAltitude));
            if (planet.Origin.SourceKey != "unknown")
                AddField(rows, "Origin", planet.Origin.DisplayName);
            if (planet.HasGeneratorMetadata)
            {
                AddField(rows, "Inherited flags", "Enabled: " + YesNo(planet.IsEnabled)
                    + "   Public: " + YesNo(planet.IsPublic)
                    + "   Survival: " + YesNo(planet.IsAvailableInSurvival));
            }
            if (planet.GeneratorId.HasValue)
            {
                AddRelationships(
                    rows,
                    "Generator",
                    new List<DetailRelationshipCandidate> { CreateDefinitionCandidate(planet.GeneratorId.Value, string.Empty) },
                    true,
                    false);
            }
        }

        #endregion

        #region Definition Sections

        private void AddRecipe(IList<DetailRowModel> rows, RecipeDocument recipe)
        {
            AddHeading(rows, "Recipe");
            AddField(rows, "Base time", recipe.BaseProductionTimeSeconds.ToString("0.###") + " s");
            AddField(rows, "Atomic", YesNo(recipe.IsAtomic));
            AddRelationships(rows, "Inputs", CreateAmountItems(recipe.Prerequisites), false, true);
            AddRelationships(rows, "Outputs", CreateAmountItems(recipe.Results), false, true);
            if (recipe.ProductionBlocks.Count > 0)
                AddRelationships(rows, "Available in production blocks", CreateProductionBlockItems(recipe.ProductionBlocks), false, false);
        }

        private void AddBlock(IList<DetailRowModel> rows, CubeBlockData block)
        {
            AddHeading(rows, "Cube block");
            AddField(rows, "Grid size", block.CubeSize.ToString());
            AddField(rows, "Dimensions", block.Size.X + " × " + block.Size.Y + " × " + block.Size.Z);
            AddField(rows, "PCU", block.Pcu.ToString());
            AddField(rows, "Listed in G menu", YesNo(block.IsBuildMenuReachable));
            if (!string.IsNullOrWhiteSpace(block.BlockPairName))
                AddField(rows, "Block pair", block.BlockPairName);
            if (block.RelatedBlocks.Count > 0)
                AddRelationships(rows, "Variants and paired sizes", CreateDefinitionItems(block.RelatedBlocks), false, false);

            var components = new List<DetailRelationshipCandidate>();
            for (int index = 0; index < block.Components.Count; index++)
            {
                BlockComponentRequirement requirement = block.Components[index];
                components.Add(CreateDefinitionCandidate(requirement.ComponentId, requirement.Count + " × "));
            }
            AddRelationships(rows, "Components", components, false, true);
        }

        private void AddPlanetGenerator(
            IList<DetailRowModel> rows,
            MyDefinitionId generatorId,
            PlanetGeneratorData planet)
        {
            AddHeading(rows, "Planet statistics");
            AddField(rows, "Surface gravity", planet.SurfaceGravity.ToString("0.###") + " g");
            AddField(rows, "Gravity falloff", planet.GravityFalloffPower.ToString("0.###"));
            AddField(rows, "Atmosphere", YesNo(planet.HasAtmosphere));
            AddField(rows, "Atmosphere height", planet.AtmosphereHeight.ToString("0.###"));
            AddField(rows, "Breathable", YesNo(planet.AtmosphereBreathable));
            AddField(rows, "Atmosphere density", planet.AtmosphereDensity.ToString("0.###"));
            AddField(rows, "Oxygen density", planet.OxygenDensity.ToString("0.###"));
            AddField(rows, "Atmosphere limit", planet.AtmosphereLimitAltitude.ToString("0.###"));
            AddField(rows, "Maximum wind", planet.MaxWindSpeed.ToString("0.###"));
            AddField(rows, "Temperature", planet.DefaultTemperature);
            AddField(rows, "Weather interval", planet.WeatherFrequencyMin + "–" + planet.WeatherFrequencyMax);
            if (!string.IsNullOrWhiteSpace(planet.PersistentWeather))
                AddField(rows, "Persistent weather", planet.PersistentWeather);

            AddRelationships(
                rows,
                "Spawned planets",
                CreateSpawnedPlanetItems(generatorId),
                false,
                false);

            var weather = new List<DetailItem>();
            for (int index = 0; index < planet.WeatherTypes.Count; index++)
                weather.Add(new DetailItem(planet.WeatherTypes[index]));
            AddPaged(rows, "Weather types", weather, false, false);

            var ores = new List<DetailRelationshipCandidate>();
            for (int index = 0; index < planet.Ores.Count; index++)
                ores.Add(CreatePlanetOreItem(planet.Ores[index]));
            AddRelationships(rows, "Ore mappings", ores, false, false);
        }

        private static void AddAsteroidGenerator(IList<DetailRowModel> rows, AsteroidGeneratorData asteroid)
        {
            AddHeading(rows, "Asteroid generation");
            AddField(rows, "Version", asteroid.Version.ToString());
            AddField(rows, "Object size", asteroid.ObjectSizeMin + "–" + asteroid.ObjectSizeMax);
            AddField(rows, "Cluster object size", asteroid.ClusterObjectSizeMin + "–" + asteroid.ClusterObjectSizeMax);
            AddField(rows, "Maximum objects", asteroid.MaxObjectsInCluster.ToString());
            AddField(rows, "Minimum spacing", asteroid.MinClusterDistance.ToString());
            AddField(rows, "Maximum spacing", asteroid.MaxClusterDistanceMin + "–" + asteroid.MaxClusterDistanceMax);
            AddField(rows, "Cluster density", asteroid.ClusterDensity.ToString("0.###"));
            AddField(rows, "Absolute dispersion", YesNo(asteroid.AbsoluteClusterDispersion));
            AddField(rows, "Rotate asteroids", YesNo(asteroid.RotateAsteroids));
            AddField(rows, "Variable cluster size", YesNo(asteroid.VariableClusterSize));
            AddStrings(rows, "Object probabilities", asteroid.SeedProbabilities);
            AddStrings(rows, "Cluster probabilities", asteroid.ClusterSeedProbabilities);
        }

        #endregion

        #region Relationship Items

        private List<DetailRelationshipCandidate> CreateSpawnedPlanetItems(MyDefinitionId generatorId)
        {
            var items = new List<DetailRelationshipCandidate>();
            if (celestial == null)
                return items;

            IReadOnlyList<PlanetSnapshot> planets = celestial.Planets;
            for (int index = 0; index < planets.Count; index++)
            {
                PlanetSnapshot planet = planets[index];
                if (!planet.GeneratorId.HasValue || planet.GeneratorId.Value != generatorId)
                    continue;

                var entry = new CatalogEntry(planet);
                items.Add(new DetailRelationshipCandidate(entry.DisplayName, entry));
            }
            return items;
        }

        private void AddReverseRelationships(IList<DetailRowModel> rows, MyDefinitionId definitionId)
        {
            IReadOnlyList<PlanetOreUsage> planetUsages = index.GetPlanetGeneratorsUsingOre(definitionId);
            var planetItems = new List<DetailRelationshipCandidate>();
            int planetUsageIndex = 0;
            while (planetUsageIndex < planetUsages.Count)
            {
                int firstUsageIndex = planetUsageIndex;
                MyDefinitionId generatorId = planetUsages[firstUsageIndex].GeneratorId;
                while (planetUsageIndex < planetUsages.Count &&
                    planetUsages[planetUsageIndex].GeneratorId == generatorId)
                    planetUsageIndex++;

                DefinitionDocument generator;
                if (!index.TryGet(generatorId, out generator))
                {
                    int unavailableMappingCount = planetUsageIndex - firstUsageIndex;
                    planetItems.Add(new DetailRelationshipCandidate(
                        generatorId + " (definition unavailable) – " +
                        unavailableMappingCount + (unavailableMappingCount == 1 ? " mapping" : " mappings")));
                    continue;
                }

                int mappingCount = planetUsageIndex - firstUsageIndex;
                string text = generator.UiDisplayName + " – " +
                    (mappingCount == 1
                        ? FormatPlanetOreMapping(planetUsages[firstUsageIndex].Mapping)
                        : mappingCount + " mappings");
                var toolTip = new StringBuilder("Ore mappings in " + generator.UiDisplayName);
                for (int mappingIndex = firstUsageIndex; mappingIndex < planetUsageIndex; mappingIndex++)
                    toolTip.Append('\n').Append(FormatPlanetOreMapping(planetUsages[mappingIndex].Mapping));
                planetItems.Add(new DetailRelationshipCandidate(text, new CatalogEntry(generator), toolTip.ToString()));
            }
            AddRelationships(rows, "Planet generators", planetItems, true, false);

            IReadOnlyList<RecipeDocument> producing = index.Recipes.GetMenuProducingRecipes(definitionId);
            if (producing.Count > 0)
                AddRelationships(rows, "Produced by recipes", CreateRecipeItems(producing, definitionId, false), true, false);
            IReadOnlyList<RecipeDocument> consuming = index.Recipes.GetMenuConsumingRecipes(definitionId);
            if (consuming.Count > 0)
                AddRelationships(rows, "Used in recipes", CreateRecipeItems(consuming, definitionId, true), true, false);

            IReadOnlyList<BlockUsage> usages = index.GetBlocksUsing(definitionId);
            var items = new List<DetailRelationshipCandidate>();
            for (int usageIndex = 0; usageIndex < usages.Count; usageIndex++)
                items.Add(CreateDefinitionCandidate(usages[usageIndex].BlockId, usages[usageIndex].Count + " × "));
            AddRelationships(rows, "Used in blocks", items, true, false);
        }

        private List<DetailRelationshipCandidate> CreateRecipeItems(
            IReadOnlyList<RecipeDocument> recipes,
            MyDefinitionId itemId,
            bool consumed)
        {
            var items = new List<DetailRelationshipCandidate>();
            for (int index = 0; index < recipes.Count; index++)
            {
                RecipeDocument recipe = recipes[index];
                MyFixedPoint amount = GetAmount(consumed ? recipe.Prerequisites : recipe.Results, itemId);
                CatalogEntry link = CreateDefinitionLink(recipe.DefinitionId);
                items.Add(new DetailRelationshipCandidate(
                    GetDefinitionName(recipe.DefinitionId) + " – " + FormatAmount(amount, itemId).TrimEnd(),
                    link,
                    BuildRecipeToolTip(recipe)));
            }
            return items;
        }

        private string BuildRecipeToolTip(RecipeDocument recipe)
        {
            var builder = new StringBuilder();
            builder.Append("Recipe: ").Append(GetDefinitionName(recipe.DefinitionId));
            AppendRecipeAmounts(builder, "Inputs", recipe.Prerequisites);
            AppendRecipeAmounts(builder, "Outputs", recipe.Results);
            return builder.ToString();
        }

        private void AppendRecipeAmounts(
            StringBuilder builder,
            string heading,
            IReadOnlyList<DefinitionAmount> amounts)
        {
            builder.Append("\n\n").Append(heading);
            if (amounts.Count == 0)
            {
                builder.Append("\nNone");
                return;
            }

            for (int index = 0; index < amounts.Count; index++)
            {
                DefinitionAmount amount = amounts[index];
                builder.Append('\n')
                    .Append(FormatAmount(amount.Amount, amount.DefinitionId))
                    .Append(GetDefinitionName(amount.DefinitionId));
            }
        }

        private List<DetailRelationshipCandidate> CreateAmountItems(IReadOnlyList<DefinitionAmount> amounts)
        {
            var items = new List<DetailRelationshipCandidate>();
            for (int index = 0; index < amounts.Count; index++)
            {
                DefinitionAmount amount = amounts[index];
                items.Add(CreateDefinitionCandidate(amount.DefinitionId, FormatAmount(amount.Amount, amount.DefinitionId)));
            }
            return items;
        }

        private List<DetailRelationshipCandidate> CreateDefinitionItems(IReadOnlyList<MyDefinitionId> ids)
        {
            var items = new List<DetailRelationshipCandidate>();
            for (int index = 0; index < ids.Count; index++)
                items.Add(CreateDefinitionCandidate(ids[index], string.Empty));
            return items;
        }

        private List<DetailRelationshipCandidate> CreateProductionBlockItems(IReadOnlyList<MyDefinitionId> ids)
        {
            var items = new List<DetailRelationshipCandidate>();
            for (int index = 0; index < ids.Count; index++)
            {
                DefinitionDocument block;
                if (!this.index.TryGet(ids[index], out block))
                {
                    items.Add(new DetailRelationshipCandidate(ids[index] + " (definition unavailable)"));
                    continue;
                }
                string grid = block.CubeBlock != null ? " – " + block.CubeBlock.CubeSize + " grid" : string.Empty;
                items.Add(new DetailRelationshipCandidate(block.UiDisplayName + grid, new CatalogEntry(block)));
            }
            return items;
        }

        private DetailRelationshipCandidate CreateDefinitionCandidate(MyDefinitionId definitionId, string prefix)
        {
            DefinitionDocument target;
            if (!index.TryGet(definitionId, out target))
                return new DetailRelationshipCandidate(prefix + definitionId + " (definition unavailable)");
            return new DetailRelationshipCandidate(prefix + target.UiDisplayName, new CatalogEntry(target));
        }

        private CatalogEntry CreateDefinitionLink(MyDefinitionId definitionId)
        {
            DefinitionDocument target;
            return index.TryGet(definitionId, out target)
                ? new CatalogEntry(target)
                : null;
        }

        private DetailRelationshipCandidate CreatePlanetOreItem(PlanetOreData mapping)
        {
            string details = FormatPlanetOreMapping(mapping);
            if (!mapping.OreId.HasValue)
                return new DetailRelationshipCandidate(details + " (ore unavailable)");

            DefinitionDocument ore;
            if (!index.TryGet(mapping.OreId.Value, out ore))
                return new DetailRelationshipCandidate(details + " (ore unavailable)");

            return new DetailRelationshipCandidate(
                mapping.Material + " → " + ore.UiDisplayName + " – " + FormatPlanetOreDepth(mapping),
                new CatalogEntry(ore));
        }

        private static string FormatPlanetOreMapping(PlanetOreData mapping)
        {
            return mapping.Material + " – " + FormatPlanetOreDepth(mapping);
        }

        private static string FormatPlanetOreDepth(PlanetOreData mapping)
        {
            return "start " + mapping.Start.ToString("0.###") +
                ", depth " + mapping.Depth.ToString("0.###");
        }

        private string GetDefinitionName(MyDefinitionId definitionId)
        {
            DefinitionDocument definition;
            return index.TryGet(definitionId, out definition)
                ? definition.UiDisplayName
                : definitionId + " (definition unavailable)";
        }

        private string FormatAmount(MyFixedPoint amount, MyDefinitionId itemId)
        {
            DefinitionDocument item;
            if (index.TryGet(itemId, out item) &&
                (item.CategoryKey == CatalogCategoryKeys.Ores || item.CategoryKey == CatalogCategoryKeys.Ingots))
                return amount + " m³ ";
            return amount + " × ";
        }

        private static MyFixedPoint GetAmount(IReadOnlyList<DefinitionAmount> amounts, MyDefinitionId itemId)
        {
            MyFixedPoint total = (MyFixedPoint)0;
            for (int index = 0; index < amounts.Count; index++)
            {
                if (amounts[index].DefinitionId == itemId)
                    total += amounts[index].Amount;
            }
            return total;
        }

        #endregion

        #region Row Factories

        private void AddRelationships(
            IList<DetailRowModel> rows,
            string heading,
            IList<DetailRelationshipCandidate> candidates,
            bool major,
            bool showEmpty)
        {
            var items = new List<DetailItem>();
            int hiddenCount = 0;
            for (int index = 0; index < candidates.Count; index++)
            {
                DetailRelationshipCandidate candidate = candidates[index];
                if (candidate.Target != null &&
                    !visibility.IsCommonlyVisible(candidate.Target, filter.Visibility))
                {
                    hiddenCount++;
                    continue;
                }
                items.Add(new DetailItem(candidate.Text, candidate.Target, candidate.ToolTip));
            }

            if (items.Count == 0 && hiddenCount == 0)
            {
                if (!showEmpty)
                    return;
                items.Add(new DetailItem("None"));
            }
            rows.Add(DetailRowModel.Paged(heading, items, major, hiddenCount));
        }

        private static void AddHeading(IList<DetailRowModel> rows, string text)
        {
            rows.Add(DetailRowModel.Heading(text));
        }

        private static void AddField(IList<DetailRowModel> rows, string label, string value)
        {
            rows.Add(DetailRowModel.Field(label, value));
        }

        private static void AddStrings(
            IList<DetailRowModel> rows,
            string heading,
            IReadOnlyList<string> values)
        {
            var items = new List<DetailItem>();
            for (int index = 0; index < values.Count; index++)
                items.Add(new DetailItem(values[index]));
            AddPaged(rows, heading, items, false, false);
        }

        private static void AddPaged(
            IList<DetailRowModel> rows,
            string heading,
            IList<DetailItem> items,
            bool major,
            bool showEmpty)
        {
            if (items.Count == 0)
            {
                if (!showEmpty)
                    return;
                items.Add(new DetailItem("None"));
            }
            rows.Add(DetailRowModel.Paged(heading, items, major, 0));
        }

        #endregion

        #region Formatting

        private static string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }

        private static string FormatDistance(float metres)
        {
            return metres >= 1000f
                ? (metres / 1000f).ToString("0.###") + " km"
                : metres.ToString("0.###") + " m";
        }

        private static string FormatVector(Vector3D value)
        {
            return value.X.ToString("0.##") + ", " + value.Y.ToString("0.##") + ", " + value.Z.ToString("0.##");
        }

        #endregion
    }
}
