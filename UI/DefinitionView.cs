using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRage;
using VRage.Game;
using VRageMath;

namespace SEpedia.UI
{
    public sealed class DefinitionView : HudElementBase
    {
        public event Action<MyDefinitionId> LinkClicked;

        private readonly DefinitionIndex index;
        private readonly ScrollBox content;
        private readonly List<HudElementBase> rows;
        private readonly List<PagedDetailSection> pagedSections;
        private readonly DefinitionHeader header;
        private float lastRowWidth;
        private bool layoutDirty;

        public DefinitionView(DefinitionIndex index, HudParentBase parent = null) : base(parent)
        {
            this.index = index;
            rows = new List<HudElementBase>();
            pagedSections = new List<PagedDetailSection>();
            lastRowWidth = -1f;
            layoutDirty = true;

            content = new ScrollBox(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Padding = new Vector2(8f),
                Spacing = 3f,
                UseSmoothScrolling = true
            };
            header = new DefinitionHeader();
            ShowMessage("Select a definition to inspect it.");
        }

        public void Show(DefinitionDocument definition)
        {
            ClearRows();
            header.Update(
                definition.DisplayName,
                definition.Id.ToString(),
                definition.RuntimeTypeName,
                definition.Description,
                definition.Icon);
            AddRow(header.Root);

            AddKeyValue("Categories", definition.BrowseCategory != BrowseCategory.None
                ? CatalogIndex.GetCategoryName(definition.BrowseCategory)
                : (definition.Categories == DefinitionCategory.None ? "Linked definition" : definition.Categories.ToString()));
            AddKeyValue("Origin", definition.Origin.DisplayName);
            AddKeyValue("Flags", "Enabled: " + YesNo(definition.Enabled)
                + "   Public: " + YesNo(definition.Public)
                + "   Survival: " + YesNo(definition.AvailableInSurvival));

            if (definition.PhysicalItem != null)
            {
                AddSection("Physical item");
                AddKeyValue("Mass", definition.PhysicalItem.Mass.ToString("0.###"));
                AddKeyValue("Volume", definition.PhysicalItem.Volume.ToString("0.######"));
            }

            if (definition.Recipe != null)
                AddRecipe(definition.Recipe);
            if (definition.CubeBlock != null)
                AddBlock(definition.CubeBlock);
            if (definition.PlanetGenerator != null)
                AddPlanetGenerator(definition.PlanetGenerator);
            if (definition.AsteroidGenerator != null)
                AddAsteroidGenerator(definition.AsteroidGenerator);

            AddReverseRelationships(definition.Id);
            content.Start = 0;
        }

        public void Show(CatalogEntry entry)
        {
            if (entry == null)
            {
                ShowMessage("Select an entry to inspect it.");
                return;
            }
            if (entry.Definition != null)
                Show(entry.Definition);
            else
                ShowPlanet(entry.Planet);
        }

        public void ShowMessage(string message)
        {
            ClearRows();
            AddParagraph(message);
        }

        protected override void Layout()
        {
            float rowWidth = Math.Max(120f, UnpaddedSize.X - content.ScrollBar.Width - content.Padding.X - 12f);
            if (!layoutDirty && Math.Abs(rowWidth - lastRowWidth) < .01f)
                return;

            for (int index = 0; index < rows.Count; index++)
            {
                rows[index].Width = rowWidth;
                Label label = rows[index] as Label;
                if (label != null)
                    label.LineWrapWidth = Math.Max(80f, rowWidth - label.Padding.X);
            }
            header.SetWidth(rowWidth);
            for (int index = 0; index < pagedSections.Count; index++)
                pagedSections[index].SetWidth(rowWidth);

            lastRowWidth = rowWidth;
            layoutDirty = false;
        }

        private void AddRecipe(RecipeDocument recipe)
        {
            AddSection("Recipe");
            AddKeyValue("Base time", recipe.BaseProductionTimeSeconds.ToString("0.###") + " s");
            AddKeyValue("Atomic", YesNo(recipe.Atomic));
            AddPagedSection("Inputs", CreateAmountItems(recipe.Prerequisites), false, true);
            AddPagedSection("Outputs", CreateAmountItems(recipe.Results), false, true);
            if (recipe.ProductionBlocks.Count > 0)
                AddPagedSection("Available in production blocks", CreateProductionBlockItems(recipe.ProductionBlocks), false, false);
        }

        private void AddBlock(CubeBlockData block)
        {
            AddSection("Cube block");
            AddKeyValue("Grid size", block.CubeSize.ToString());
            AddKeyValue("Dimensions", block.Size.X + " × " + block.Size.Y + " × " + block.Size.Z);
            AddKeyValue("PCU", block.Pcu.ToString());
            AddKeyValue("GUI visible", YesNo(block.GuiVisible));
            AddKeyValue("G-menu reachable", YesNo(block.BuildMenuReachable));
            if (!string.IsNullOrWhiteSpace(block.BlockPairName))
                AddKeyValue("Block pair", block.BlockPairName);

            if (block.RelatedBlocks.Count > 0)
                AddPagedSection("Variants and paired sizes", CreateDefinitionItems(block.RelatedBlocks), false, false);

            var components = new List<DetailItem>();
            for (int componentIndex = 0; componentIndex < block.Components.Count; componentIndex++)
            {
                BlockComponentRequirement requirement = block.Components[componentIndex];
                components.Add(CreateDefinitionItem(requirement.ComponentId, requirement.Count + " × "));
            }
            AddPagedSection("Components", components, false, true);
        }

        private void ShowPlanet(PlanetSnapshot planet)
        {
            ClearRows();
            DefinitionIconData icon = null;
            DefinitionDocument generator;
            if (planet.GeneratorId.HasValue && index.TryGet(planet.GeneratorId.Value, out generator))
                icon = generator.Icon;
            header.Update(
                planet.DisplayName,
                planet.EntityId.ToString(),
                "Spawned planet",
                string.Empty,
                icon);
            AddRow(header.Root);

            AddKeyValue("Position", FormatVector(planet.Position));
            AddKeyValue("Minimum radius", FormatDistance(planet.MinimumRadius));
            AddKeyValue("Average radius", FormatDistance(planet.AverageRadius));
            AddKeyValue("Maximum radius", FormatDistance(planet.MaximumRadius));
            AddKeyValue("Has atmosphere", YesNo(planet.HasAtmosphere));
            AddKeyValue("Atmosphere radius", FormatDistance(planet.AtmosphereRadius));
            AddKeyValue("Atmosphere altitude", FormatDistance(planet.AtmosphereAltitude));
            if (planet.Origin.SourceKey != "unknown")
                AddKeyValue("Origin", planet.Origin.DisplayName);
            if (planet.HasGeneratorMetadata)
            {
                AddKeyValue("Inherited flags", "Enabled: " + YesNo(planet.Enabled)
                    + "   Public: " + YesNo(planet.Public)
                    + "   Survival: " + YesNo(planet.AvailableInSurvival));
            }
            if (planet.GeneratorId.HasValue)
            {
                AddSection("Generator");
                AddDefinitionLink(planet.GeneratorId.Value, string.Empty);
            }
            if (planet.GeneratorData != null)
                AddPlanetGenerator(planet.GeneratorData);
            content.Start = 0;
        }

        private void AddPlanetGenerator(PlanetGeneratorData planet)
        {
            AddSection("Planet statistics");
            AddKeyValue("Surface gravity", planet.SurfaceGravity.ToString("0.###") + " g");
            AddKeyValue("Gravity falloff", planet.GravityFalloffPower.ToString("0.###"));
            AddKeyValue("Atmosphere", planet.HasAtmosphere ? "Yes" : "No");
            AddKeyValue("Atmosphere height", planet.AtmosphereHeight.ToString("0.###"));
            AddKeyValue("Breathable", YesNo(planet.AtmosphereBreathable));
            AddKeyValue("Atmosphere density", planet.AtmosphereDensity.ToString("0.###"));
            AddKeyValue("Oxygen density", planet.OxygenDensity.ToString("0.###"));
            AddKeyValue("Atmosphere limit", planet.AtmosphereLimitAltitude.ToString("0.###"));
            AddKeyValue("Maximum wind", planet.MaxWindSpeed.ToString("0.###"));
            AddKeyValue("Temperature", planet.DefaultTemperature);
            AddKeyValue("Weather interval", planet.WeatherFrequencyMin + "–" + planet.WeatherFrequencyMax);
            if (!string.IsNullOrWhiteSpace(planet.PersistentWeather))
                AddKeyValue("Persistent weather", planet.PersistentWeather);

            var weather = new List<DetailItem>();
            for (int index = 0; index < planet.WeatherTypes.Count; index++)
                weather.Add(new DetailItem(planet.WeatherTypes[index]));
            if (weather.Count > 0)
                AddPagedSection("Weather types", weather, false, false);

            var ores = new List<DetailItem>();
            for (int index = 0; index < planet.Ores.Count; index++)
            {
                PlanetOreData ore = planet.Ores[index];
                ores.Add(new DetailItem(ore.Material + " — start " + ore.Start.ToString("0.###") +
                    ", depth " + ore.Depth.ToString("0.###")));
            }
            if (ores.Count > 0)
                AddPagedSection("Ore mappings", ores, false, false);
        }

        private void AddAsteroidGenerator(AsteroidGeneratorData asteroid)
        {
            AddSection("Asteroid generation");
            AddKeyValue("Version", asteroid.Version.ToString());
            AddKeyValue("Object size", asteroid.ObjectSizeMin + "–" + asteroid.ObjectSizeMax);
            AddKeyValue("Cluster object size", asteroid.ClusterObjectSizeMin + "–" + asteroid.ClusterObjectSizeMax);
            AddKeyValue("Maximum objects", asteroid.MaxObjectsInCluster.ToString());
            AddKeyValue("Minimum spacing", asteroid.MinClusterDistance.ToString());
            AddKeyValue("Maximum spacing", asteroid.MaxClusterDistanceMin + "–" + asteroid.MaxClusterDistanceMax);
            AddKeyValue("Cluster density", asteroid.ClusterDensity.ToString("0.###"));
            AddKeyValue("Absolute dispersion", YesNo(asteroid.AbsoluteClusterDispersion));
            AddKeyValue("Rotate asteroids", YesNo(asteroid.RotateAsteroids));
            AddKeyValue("Variable cluster size", YesNo(asteroid.VariableClusterSize));
            AddStringSection("Object probabilities", asteroid.SeedProbabilities);
            AddStringSection("Cluster probabilities", asteroid.ClusterSeedProbabilities);
        }

        private void AddStringSection(string headingText, IReadOnlyList<string> values)
        {
            if (values.Count == 0)
                return;
            var items = new List<DetailItem>();
            for (int index = 0; index < values.Count; index++)
                items.Add(new DetailItem(values[index]));
            AddPagedSection(headingText, items, false, false);
        }

        private void AddReverseRelationships(MyDefinitionId definitionId)
        {
            IReadOnlyList<RecipeDocument> producing = index.Recipes.GetMenuProducingRecipes(definitionId);
            if (producing.Count > 0)
                AddPagedSection("Produced by recipes", CreateRecipeItems(producing, definitionId, false), true, false);

            IReadOnlyList<RecipeDocument> consuming = index.Recipes.GetMenuConsumingRecipes(definitionId);
            if (consuming.Count > 0)
                AddPagedSection("Used in recipes", CreateRecipeItems(consuming, definitionId, true), true, false);

            IReadOnlyList<BlockUsage> usages = index.GetBlocksUsing(definitionId);
            if (usages.Count > 0)
            {
                var items = new List<DetailItem>();
                for (int usageIndex = 0; usageIndex < usages.Count; usageIndex++)
                    items.Add(CreateDefinitionItem(usages[usageIndex].BlockId, usages[usageIndex].Count + " × "));
                AddPagedSection("Used in blocks", items, true, false);
            }
        }

        private List<DetailItem> CreateRecipeItems(
            IReadOnlyList<RecipeDocument> recipes,
            MyDefinitionId itemId,
            bool consumed)
        {
            var items = new List<DetailItem>();
            for (int recipeIndex = 0; recipeIndex < recipes.Count; recipeIndex++)
            {
                RecipeDocument recipe = recipes[recipeIndex];
                MyFixedPoint amount = GetAmount(consumed ? recipe.Prerequisites : recipe.Results, itemId);
                DetailItem item = CreateDefinitionItem(recipe.DefinitionId, FormatAmount(amount, itemId));
                if (item.LinkId.HasValue)
                    item = new DetailItem(item.Text + " — " + BuildRecipeSummary(recipe), item.LinkId);
                items.Add(item);
            }
            return items;
        }

        private List<DetailItem> CreateAmountItems(IReadOnlyList<DefinitionAmount> amounts)
        {
            var items = new List<DetailItem>();
            for (int amountIndex = 0; amountIndex < amounts.Count; amountIndex++)
            {
                DefinitionAmount amount = amounts[amountIndex];
                items.Add(CreateDefinitionItem(amount.DefinitionId, FormatAmount(amount.Amount, amount.DefinitionId)));
            }
            return items;
        }

        private List<DetailItem> CreateDefinitionItems(IReadOnlyList<MyDefinitionId> ids)
        {
            var items = new List<DetailItem>();
            for (int itemIndex = 0; itemIndex < ids.Count; itemIndex++)
                items.Add(CreateDefinitionItem(ids[itemIndex], string.Empty));
            return items;
        }

        private List<DetailItem> CreateProductionBlockItems(IReadOnlyList<MyDefinitionId> ids)
        {
            var items = new List<DetailItem>();
            for (int itemIndex = 0; itemIndex < ids.Count; itemIndex++)
            {
                DefinitionDocument block;
                if (!index.TryGet(ids[itemIndex], out block))
                {
                    items.Add(new DetailItem(ids[itemIndex] + " (definition unavailable)"));
                    continue;
                }

                string grid = block.CubeBlock != null ? " — " + block.CubeBlock.CubeSize + " grid" : string.Empty;
                items.Add(new DetailItem(block.DisplayName + grid, block.Id));
            }
            return items;
        }

        private string BuildRecipeSummary(RecipeDocument recipe)
        {
            return JoinAmountNames(recipe.Prerequisites, 2) + " → " + JoinAmountNames(recipe.Results, 2);
        }

        private string JoinAmountNames(IReadOnlyList<DefinitionAmount> amounts, int limit)
        {
            if (amounts.Count == 0)
                return "None";

            var names = new List<string>();
            int count = Math.Min(amounts.Count, limit);
            for (int itemIndex = 0; itemIndex < count; itemIndex++)
            {
                DefinitionDocument item;
                names.Add(index.TryGet(amounts[itemIndex].DefinitionId, out item)
                    ? item.DisplayName
                    : amounts[itemIndex].DefinitionId.SubtypeName);
            }
            string result = string.Join(" + ", names);
            return amounts.Count > limit ? result + " + …" : result;
        }

        private DetailItem CreateDefinitionItem(MyDefinitionId definitionId, string prefix)
        {
            DefinitionDocument target;
            if (!index.TryGet(definitionId, out target))
                return new DetailItem(prefix + definitionId + " (definition unavailable)");
            return new DetailItem(prefix + target.DisplayName, definitionId);
        }

        private void AddDefinitionLink(MyDefinitionId definitionId, string prefix)
        {
            DetailItem item = CreateDefinitionItem(definitionId, prefix);
            if (!item.LinkId.HasValue)
            {
                AddParagraph(item.Text);
                return;
            }

            MyDefinitionId capturedId = item.LinkId.Value;
            var link = new LabelButton
            {
                Text = new RichText(item.Text, GlyphFormat.Blueish.WithStyle(FontStyles.Underline).WithSize(.88f)),
                BuilderMode = TextBuilderModes.Wrapped,
                AutoResize = true,
                VertCenterText = false,
                Padding = new Vector2(12f, 2f)
            };
            link.MouseInput.LeftClicked += delegate { RaiseLinkClicked(capturedId); };
            AddRow(link);
        }

        private void AddPagedSection(
            string headingText,
            IList<DetailItem> items,
            bool majorHeading,
            bool showEmpty)
        {
            if (items.Count == 0)
            {
                if (!showEmpty)
                    return;
                items.Add(new DetailItem("None"));
            }
            var section = new PagedDetailSection(RaiseLinkClicked, headingText, items, majorHeading);
            pagedSections.Add(section);
            AddRow(section.Root);
        }

        private void AddSection(string text)
        {
            AddLabel(new RichText(text, GlyphFormat.Blueish.WithSize(1.02f)), 31f, new Vector2(8f, 7f));
        }

        private void AddKeyValue(string key, string value)
        {
            var text = new RichText();
            text.Add(key + ": ", GlyphFormat.Blueish.WithSize(.82f));
            text.Add(value ?? string.Empty, GlyphFormat.White.WithSize(.82f));
            AddLabel(text, 24f, new Vector2(8f, 2f));
        }

        private void AddParagraph(string text)
        {
            var label = new Label
            {
                Text = new RichText(text ?? string.Empty, GlyphFormat.White.WithSize(.82f)),
                BuilderMode = TextBuilderModes.Wrapped,
                AutoResize = true,
                VertCenterText = false,
                Padding = new Vector2(8f, 4f)
            };
            AddRow(label);
        }

        private void AddLabel(RichText text, float height, Vector2 padding)
        {
            var label = new Label
            {
                Text = text,
                Height = height,
                AutoResize = false,
                VertCenterText = true,
                Padding = padding
            };
            AddRow(label);
        }

        private void AddRow(HudElementBase row)
        {
            rows.Add(row);
            content.Add(row);
            layoutDirty = true;
        }

        private void ClearRows()
        {
            content.Clear();
            rows.Clear();
            pagedSections.Clear();
            layoutDirty = true;
        }

        private void RaiseLinkClicked(MyDefinitionId id)
        {
            Action<MyDefinitionId> handler = LinkClicked;
            if (handler != null)
                handler(id);
        }

        private string FormatAmount(MyFixedPoint amount, MyDefinitionId itemId)
        {
            DefinitionDocument item;
            if (index.TryGet(itemId, out item) &&
                (item.Categories & (DefinitionCategory.Ore | DefinitionCategory.Ingot)) != 0)
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
    }
}
