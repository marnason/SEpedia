using System;
using Sandbox.Definitions;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class DefinitionExtractors
    {
        #region State and Construction

        private readonly DefinitionRelationships relationships;
        private readonly DefinitionBuildDiagnostics diagnostics;
        private readonly PlanetOreResolver planetOreResolver;

        public DefinitionExtractors(
            MyDefinitionManager manager,
            DefinitionRelationships relationships,
            DefinitionBuildDiagnostics diagnostics)
        {
            this.relationships = relationships;
            this.diagnostics = diagnostics;
            planetOreResolver = new PlanetOreResolver(manager, diagnostics);
        }

        #endregion

        #region Definition Extraction

        public DefinitionDocument Extract(MyDefinitionBase definition)
        {
            MyDefinitionId id = definition.Id;
            string categoryKey = string.Empty;
            PhysicalItemData physical = null;
            RecipeDocument recipe = null;
            CubeBlockData block = null;
            PlanetGeneratorData planet = null;
            AsteroidGeneratorData asteroid = null;

            MyPhysicalItemDefinition physicalDefinition = definition as MyPhysicalItemDefinition;
            if (physicalDefinition != null)
            {
                physical = PhysicalDefinitionExtractor.Extract(physicalDefinition, diagnostics);
                categoryKey = GetPhysicalCategoryKey(physicalDefinition);
            }

            MyBlueprintDefinitionBase blueprintDefinition = definition as MyBlueprintDefinitionBase;
            if (blueprintDefinition != null)
            {
                recipe = ProductionDefinitionExtractor.Extract(
                    blueprintDefinition,
                    relationships,
                    diagnostics);
                if (recipe != null && recipe.IsProductionMenuReachable)
                    categoryKey = CatalogCategoryKeys.Recipes;
            }

            MyCubeBlockDefinition blockDefinition = definition as MyCubeBlockDefinition;
            if (blockDefinition != null)
            {
                categoryKey = CatalogCategoryKeys.Blocks;
                block = CubeBlockDefinitionExtractor.Extract(
                    blockDefinition,
                    relationships,
                    diagnostics);
            }

            MyPlanetGeneratorDefinition planetDefinition = definition as MyPlanetGeneratorDefinition;
            if (planetDefinition != null)
            {
                categoryKey = CatalogCategoryKeys.Celestial;
                planet = CelestialDefinitionExtractor.ExtractPlanet(
                    planetDefinition,
                    planetOreResolver,
                    diagnostics);
            }

            MyAsteroidGeneratorDefinition asteroidDefinition = definition as MyAsteroidGeneratorDefinition;
            if (asteroidDefinition != null)
            {
                categoryKey = CatalogCategoryKeys.Celestial;
                asteroid = CelestialDefinitionExtractor.ExtractAsteroid(asteroidDefinition, diagnostics);
            }

            return new DefinitionDocument(
                id,
                GetDisplayName(definition, id),
                GetDescription(definition),
                definition.GetType().FullName ?? definition.GetType().Name,
                categoryKey,
                GetOrigin(definition),
                definition.Enabled,
                definition.Public,
                definition.AvailableInSurvival,
                physical,
                recipe,
                block,
                planet,
                asteroid);
        }

        #endregion

        #region Safe Metadata Access

        private DefinitionOrigin GetOrigin(MyDefinitionBase definition)
        {
            try
            {
                MyModContext context = definition.Context;
                return context == null
                    ? DefinitionOrigin.Unknown
                    : new DefinitionOrigin(
                        context.IsBaseGame,
                        context.ModName,
                        context.ModId,
                        context.ModServiceName);
            }
            catch (Exception exception)
            {
                diagnostics.Report("definition-origin", "Could not read " + definition.Id, exception);
                return DefinitionOrigin.Unknown;
            }
        }

        private string GetDisplayName(MyDefinitionBase definition, MyDefinitionId id)
        {
            if (HasMisleadingTreeObjectName(definition, id))
                return !string.IsNullOrWhiteSpace(id.SubtypeName) ? id.SubtypeName : id.ToString();

            try
            {
                string displayName = definition.DisplayNameText;
                if (!string.IsNullOrWhiteSpace(displayName) &&
                    !IsBareAsteroidGeneratorName(definition, id, displayName))
                    return displayName;
            }
            catch (Exception exception)
            {
                diagnostics.Report("display-name", "Could not read primary name for " + id, exception);
            }

            try
            {
                string displayName = definition.DisplayNameString;
                if (!string.IsNullOrWhiteSpace(displayName) &&
                    !IsBareAsteroidGeneratorName(definition, id, displayName))
                    return displayName;
            }
            catch (Exception exception)
            {
                diagnostics.Report("display-name-fallback", "Could not read fallback name for " + id, exception);
            }

            if (definition is MyAsteroidGeneratorDefinition && !string.IsNullOrWhiteSpace(id.SubtypeName))
                return "Asteroid generator " + id.SubtypeName;

            return !string.IsNullOrWhiteSpace(id.SubtypeName) ? id.SubtypeName : id.ToString();
        }

        private static bool HasMisleadingTreeObjectName(MyDefinitionBase definition, MyDefinitionId id)
        {
            return IsTreeObject(id) &&
                definition.DisplayNameEnum.HasValue &&
                string.Equals(
                    definition.DisplayNameEnum.Value.ToString(),
                    "DisplayName_Item_Welder",
                    StringComparison.Ordinal);
        }

        private static bool IsBareAsteroidGeneratorName(
            MyDefinitionBase definition,
            MyDefinitionId id,
            string displayName)
        {
            return definition is MyAsteroidGeneratorDefinition &&
                (string.Equals(displayName, id.SubtypeName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(displayName, id.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        private string GetDescription(MyDefinitionBase definition)
        {
            try
            {
                return definition.DescriptionText ?? string.Empty;
            }
            catch (Exception exception)
            {
                diagnostics.Report("description", "Could not read primary description for " + definition.Id, exception);
            }

            try
            {
                return definition.DescriptionString ?? string.Empty;
            }
            catch (Exception exception)
            {
                diagnostics.Report("description-fallback", "Could not read fallback description for " + definition.Id, exception);
                return string.Empty;
            }
        }

        #endregion

        #region Category Selection

        private static string GetPhysicalCategoryKey(MyPhysicalItemDefinition definition)
        {
            if (IsTreeObject(definition.Id)) return string.Empty;
            if (definition is MyComponentDefinition) return CatalogCategoryKeys.Components;
            if (definition.IsOre) return CatalogCategoryKeys.Ores;
            if (definition.IsIngot) return CatalogCategoryKeys.Ingots;
            if (definition is MyAmmoMagazineDefinition) return CatalogCategoryKeys.Ammo;
            return CatalogCategoryKeys.ToolsGearAndSupplies;
        }

        private static bool IsTreeObject(MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_TreeObject);
        }

        #endregion
    }
}
