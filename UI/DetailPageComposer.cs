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

        public DetailPageComposer(DefinitionIndex index)
        {
            this.index = index;
        }

        #endregion

        #region Page Composition

        public DetailPageModel Compose(DefinitionDocument definition)
        {
            var rows = new List<DetailRowModel>();
            AddField(rows, "Origin", definition.Origin.DisplayName);
            AddField(rows, "Flags", "Enabled: " + YesNo(definition.IsEnabled)
                + "   Public: " + YesNo(definition.IsPublic)
                + "   Survival: " + YesNo(definition.IsAvailableInSurvival));

            if (definition.PhysicalItem != null)
            {
                AddHeading(rows, "Physical item");
                AddField(rows, "Mass", definition.PhysicalItem.Mass.ToString("0.###"));
                AddField(rows, "Volume", definition.PhysicalItem.Volume.ToString("0.######"));
            }
            if (definition.Recipe != null)
                AddRecipe(rows, definition.Recipe);
            if (definition.CubeBlock != null)
                AddBlock(rows, definition.CubeBlock);
            if (definition.PlanetGenerator != null)
                AddPlanetGenerator(rows, definition.PlanetGenerator);
            if (definition.AsteroidGenerator != null)
                AddAsteroidGenerator(rows, definition.AsteroidGenerator);
            AddReverseRelationships(rows, definition.Id);

            return new DetailPageModel(
                definition.UiDisplayName,
                definition.Id.ToString(),
                definition.RuntimeTypeName,
                definition.Description,
                rows);
        }

        public DetailPageModel Compose(PlanetSnapshot planet)
        {
            var rows = new List<DetailRowModel>();
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
                AddPaged(
                    rows,
                    "Generator",
                    new List<DetailItem> { CreateDefinitionItem(planet.GeneratorId.Value, string.Empty) },
                    true,
                    false);
            }
            if (planet.GeneratorData != null)
                AddPlanetGenerator(rows, planet.GeneratorData);

            return new DetailPageModel(
                planet.DisplayName,
                planet.EntityId.ToString(),
                "Spawned planet",
                string.Empty,
                rows);
        }

        #endregion

        #region Definition Sections

        private void AddRecipe(IList<DetailRowModel> rows, RecipeDocument recipe)
        {
            AddHeading(rows, "Recipe");
            AddField(rows, "Base time", recipe.BaseProductionTimeSeconds.ToString("0.###") + " s");
            AddField(rows, "Atomic", YesNo(recipe.IsAtomic));
            AddPaged(rows, "Inputs", CreateAmountItems(recipe.Prerequisites), false, true);
            AddPaged(rows, "Outputs", CreateAmountItems(recipe.Results), false, true);
            if (recipe.ProductionBlocks.Count > 0)
                AddPaged(rows, "Available in production blocks", CreateProductionBlockItems(recipe.ProductionBlocks), false, false);
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
                AddPaged(rows, "Variants and paired sizes", CreateDefinitionItems(block.RelatedBlocks), false, false);

            var components = new List<DetailItem>();
            for (int index = 0; index < block.Components.Count; index++)
            {
                BlockComponentRequirement requirement = block.Components[index];
                components.Add(CreateDefinitionItem(requirement.ComponentId, requirement.Count + " × "));
            }
            AddPaged(rows, "Components", components, false, true);
        }

        private static void AddPlanetGenerator(IList<DetailRowModel> rows, PlanetGeneratorData planet)
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

            var weather = new List<DetailItem>();
            for (int index = 0; index < planet.WeatherTypes.Count; index++)
                weather.Add(new DetailItem(planet.WeatherTypes[index]));
            AddPaged(rows, "Weather types", weather, false, false);

            var ores = new List<DetailItem>();
            for (int index = 0; index < planet.Ores.Count; index++)
            {
                PlanetOreData ore = planet.Ores[index];
                ores.Add(new DetailItem(ore.Material + " – start " + ore.Start.ToString("0.###")
                    + ", depth " + ore.Depth.ToString("0.###")));
            }
            AddPaged(rows, "Ore mappings", ores, false, false);
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

        private void AddReverseRelationships(IList<DetailRowModel> rows, MyDefinitionId definitionId)
        {
            IReadOnlyList<RecipeDocument> producing = index.Recipes.GetMenuProducingRecipes(definitionId);
            if (producing.Count > 0)
                AddPaged(rows, "Produced by recipes", CreateRecipeItems(producing, definitionId, false), true, false);
            IReadOnlyList<RecipeDocument> consuming = index.Recipes.GetMenuConsumingRecipes(definitionId);
            if (consuming.Count > 0)
                AddPaged(rows, "Used in recipes", CreateRecipeItems(consuming, definitionId, true), true, false);

            IReadOnlyList<BlockUsage> usages = index.GetBlocksUsing(definitionId);
            var items = new List<DetailItem>();
            for (int usageIndex = 0; usageIndex < usages.Count; usageIndex++)
                items.Add(CreateDefinitionItem(usages[usageIndex].BlockId, usages[usageIndex].Count + " × "));
            AddPaged(rows, "Used in blocks", items, true, false);
        }

        private List<DetailItem> CreateRecipeItems(
            IReadOnlyList<RecipeDocument> recipes,
            MyDefinitionId itemId,
            bool consumed)
        {
            var items = new List<DetailItem>();
            for (int index = 0; index < recipes.Count; index++)
            {
                RecipeDocument recipe = recipes[index];
                MyFixedPoint amount = GetAmount(consumed ? recipe.Prerequisites : recipe.Results, itemId);
                items.Add(new DetailItem(
                    GetDefinitionName(recipe.DefinitionId) + " – " + FormatAmount(amount, itemId).TrimEnd(),
                    recipe.DefinitionId,
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

        private List<DetailItem> CreateAmountItems(IReadOnlyList<DefinitionAmount> amounts)
        {
            var items = new List<DetailItem>();
            for (int index = 0; index < amounts.Count; index++)
            {
                DefinitionAmount amount = amounts[index];
                items.Add(CreateDefinitionItem(amount.DefinitionId, FormatAmount(amount.Amount, amount.DefinitionId)));
            }
            return items;
        }

        private List<DetailItem> CreateDefinitionItems(IReadOnlyList<MyDefinitionId> ids)
        {
            var items = new List<DetailItem>();
            for (int index = 0; index < ids.Count; index++)
                items.Add(CreateDefinitionItem(ids[index], string.Empty));
            return items;
        }

        private List<DetailItem> CreateProductionBlockItems(IReadOnlyList<MyDefinitionId> ids)
        {
            var items = new List<DetailItem>();
            for (int index = 0; index < ids.Count; index++)
            {
                DefinitionDocument block;
                if (!this.index.TryGet(ids[index], out block))
                {
                    items.Add(new DetailItem(ids[index] + " (definition unavailable)"));
                    continue;
                }
                string grid = block.CubeBlock != null ? " – " + block.CubeBlock.CubeSize + " grid" : string.Empty;
                items.Add(new DetailItem(block.UiDisplayName + grid, block.Id));
            }
            return items;
        }

        private DetailItem CreateDefinitionItem(MyDefinitionId definitionId, string prefix)
        {
            DefinitionDocument target;
            if (!index.TryGet(definitionId, out target))
                return new DetailItem(prefix + definitionId + " (definition unavailable)");
            return new DetailItem(prefix + target.UiDisplayName, definitionId);
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
                (item.BrowseCategory == BrowseCategory.Ores || item.BrowseCategory == BrowseCategory.Ingots))
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
            rows.Add(DetailRowModel.Paged(heading, items, major));
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
