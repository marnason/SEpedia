using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
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
        private float lastRowWidth;
        private bool layoutDirty;

        public DefinitionView(DefinitionIndex index, HudParentBase parent = null) : base(parent)
        {
            this.index = index;
            rows = new List<HudElementBase>();
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

            ShowMessage("Select a definition to inspect it.");
        }

        public void Show(DefinitionDocument definition)
        {
            ClearRows();

            AddHeading(definition.DisplayName, definition.Id.ToString(), definition.RuntimeTypeName);

            if (!string.IsNullOrWhiteSpace(definition.Description))
                AddDescription(definition.Description);

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

            lastRowWidth = rowWidth;
            layoutDirty = false;
        }

        private void AddRecipe(RecipeDocument recipe)
        {
            AddSection("Recipe");
            AddKeyValue("Base time", recipe.BaseProductionTimeSeconds.ToString("0.###") + " s");
            AddKeyValue("Atomic", YesNo(recipe.Atomic));
            AddRelationshipAmounts("Prerequisites", recipe.Prerequisites);
            AddRelationshipAmounts("Results", recipe.Results);
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
            {
                AddSubheading("Variants and paired sizes");
                for (int index = 0; index < block.RelatedBlocks.Count; index++)
                    AddDefinitionLink(block.RelatedBlocks[index], string.Empty);
            }

            AddSubheading("Components");
            if (block.Components.Count == 0)
            {
                AddParagraph("No component requirements were registered.");
                return;
            }

            for (int componentIndex = 0; componentIndex < block.Components.Count; componentIndex++)
            {
                BlockComponentRequirement requirement = block.Components[componentIndex];
                AddDefinitionLink(requirement.ComponentId, requirement.Count + " × ");
            }
        }

        private void ShowPlanet(PlanetSnapshot planet)
        {
            ClearRows();
            AddHeading(planet.DisplayName, planet.EntityId.ToString(), "Spawned planet");
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
            if (planet.WeatherTypes.Count > 0)
            {
                AddSubheading("Weather types");
                for (int index = 0; index < planet.WeatherTypes.Count; index++)
                    AddParagraph(planet.WeatherTypes[index]);
            }
            if (planet.Ores.Count > 0)
            {
                AddSubheading("Ore mappings");
                for (int index = 0; index < planet.Ores.Count; index++)
                {
                    PlanetOreData ore = planet.Ores[index];
                    AddParagraph(ore.Material + " — start " + ore.Start.ToString("0.###") + ", depth " + ore.Depth.ToString("0.###"));
                }
            }
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
            AddStringList("Object probabilities", asteroid.SeedProbabilities);
            AddStringList("Cluster probabilities", asteroid.ClusterSeedProbabilities);
        }

        private void AddStringList(string heading, IReadOnlyList<string> values)
        {
            if (values.Count == 0)
                return;
            AddSubheading(heading);
            for (int index = 0; index < values.Count; index++)
                AddParagraph(values[index]);
        }

        private void AddReverseRelationships(MyDefinitionId definitionId)
        {
            IReadOnlyList<RecipeDocument> producing = index.Recipes.GetProducingRecipes(definitionId);
            if (producing.Count > 0)
            {
                AddSection("Produced by");
                for (int recipeIndex = 0; recipeIndex < producing.Count; recipeIndex++)
                    AddDefinitionLink(producing[recipeIndex].DefinitionId, string.Empty);
            }

            IReadOnlyList<RecipeDocument> consuming = index.Recipes.GetConsumingRecipes(definitionId);
            if (consuming.Count > 0)
            {
                AddSection("Consumed by");
                for (int recipeIndex = 0; recipeIndex < consuming.Count; recipeIndex++)
                    AddDefinitionLink(consuming[recipeIndex].DefinitionId, string.Empty);
            }

            IReadOnlyList<BlockUsage> usages = index.GetBlocksUsing(definitionId);
            if (usages.Count > 0)
            {
                AddSection("Used in blocks");
                for (int usageIndex = 0; usageIndex < usages.Count; usageIndex++)
                    AddDefinitionLink(usages[usageIndex].BlockId, usages[usageIndex].Count + " × ");
            }
        }

        private void AddRelationshipAmounts(string heading, IReadOnlyList<DefinitionAmount> amounts)
        {
            AddSubheading(heading);

            if (amounts.Count == 0)
            {
                AddParagraph("None");
                return;
            }

            for (int amountIndex = 0; amountIndex < amounts.Count; amountIndex++)
                AddDefinitionLink(amounts[amountIndex].DefinitionId, amounts[amountIndex].Amount + " × ");
        }

        private void AddDefinitionLink(MyDefinitionId definitionId, string prefix)
        {
            DefinitionDocument target;
            if (!index.TryGet(definitionId, out target))
            {
                AddParagraph(prefix + definitionId + " (definition unavailable)");
                return;
            }

            MyDefinitionId capturedId = definitionId;
            var link = new LabelButton
            {
                Text = new RichText(prefix + target.DisplayName, GlyphFormat.Blueish.WithStyle(FontStyles.Underline).WithSize(.88f)),
                BuilderMode = TextBuilderModes.Wrapped,
                AutoResize = true,
                VertCenterText = false,
                Padding = new Vector2(12f, 2f)
            };
            link.MouseInput.LeftClicked += delegate
            {
                if (LinkClicked != null)
                    LinkClicked(capturedId);
            };

            AddRow(link);
        }

        private void AddHeading(string text, string id, string type)
        {
            var label = new Label
            {
                Text = new RichText(text, GlyphFormat.White.WithSize(1.25f)),
                Height = 36f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(8f, 4f)
            };
            new MouseInputElement(label)
            {
                ToolTip = "ID: " + (id ?? string.Empty) + "\nType: " + (type ?? string.Empty)
            };
            AddRow(label);
        }

        private void AddDescription(string text)
        {
            var label = new Label
            {
                Text = new RichText(text ?? string.Empty, GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.82f)),
                BuilderMode = TextBuilderModes.Wrapped,
                AutoResize = true,
                VertCenterText = false,
                Padding = new Vector2(18f, 4f)
            };
            AddRow(label);
        }

        private void AddSection(string text)
        {
            AddLabel(new RichText(text, GlyphFormat.Blueish.WithSize(1.02f)), 31f, new Vector2(8f, 7f));
        }

        private void AddSubheading(string text)
        {
            AddLabel(new RichText(text, GlyphFormat.White.WithSize(.92f)), 25f, new Vector2(8f, 3f));
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
            layoutDirty = true;
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

        private static string FormatVector(VRageMath.Vector3D value)
        {
            return value.X.ToString("0.##") + ", " + value.Y.ToString("0.##") + ", " + value.Z.ToString("0.##");
        }
    }
}
