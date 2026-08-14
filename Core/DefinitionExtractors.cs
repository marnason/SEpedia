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

        public DefinitionExtractors(
            DefinitionRelationships relationships,
            DefinitionBuildDiagnostics diagnostics)
        {
            this.relationships = relationships;
            this.diagnostics = diagnostics;
        }

        #endregion

        #region Definition Extraction

        public DefinitionDocument Extract(MyDefinitionBase definition)
        {
            MyDefinitionId id = definition.Id;
            BrowseCategory browseCategory = BrowseCategory.None;
            PhysicalItemData physical = null;
            RecipeDocument recipe = null;
            CubeBlockData block = null;
            PlanetGeneratorData planet = null;
            AsteroidGeneratorData asteroid = null;

            MyPhysicalItemDefinition physicalDefinition = definition as MyPhysicalItemDefinition;
            if (physicalDefinition != null)
            {
                physical = PhysicalDefinitionExtractor.Extract(physicalDefinition, diagnostics);
                browseCategory = GetPhysicalBrowseCategory(physicalDefinition);
            }

            MyBlueprintDefinitionBase blueprintDefinition = definition as MyBlueprintDefinitionBase;
            if (blueprintDefinition != null)
            {
                recipe = ProductionDefinitionExtractor.Extract(
                    blueprintDefinition,
                    relationships,
                    diagnostics);
                if (recipe != null && recipe.IsProductionMenuReachable)
                    browseCategory = BrowseCategory.Recipes;
            }

            MyCubeBlockDefinition blockDefinition = definition as MyCubeBlockDefinition;
            if (blockDefinition != null)
            {
                browseCategory = BrowseCategory.Blocks;
                block = CubeBlockDefinitionExtractor.Extract(
                    blockDefinition,
                    relationships,
                    diagnostics);
            }

            MyPlanetGeneratorDefinition planetDefinition = definition as MyPlanetGeneratorDefinition;
            if (planetDefinition != null)
            {
                browseCategory = BrowseCategory.Celestial;
                planet = CelestialDefinitionExtractor.ExtractPlanet(planetDefinition, diagnostics);
            }

            MyAsteroidGeneratorDefinition asteroidDefinition = definition as MyAsteroidGeneratorDefinition;
            if (asteroidDefinition != null)
            {
                browseCategory = BrowseCategory.Celestial;
                asteroid = CelestialDefinitionExtractor.ExtractAsteroid(asteroidDefinition, diagnostics);
            }

            return new DefinitionDocument(
                id,
                GetDisplayName(definition, id),
                GetDescription(definition),
                definition.GetType().FullName ?? definition.GetType().Name,
                browseCategory,
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
            try
            {
                if (!string.IsNullOrWhiteSpace(definition.DisplayNameText))
                    return definition.DisplayNameText;
            }
            catch (Exception exception)
            {
                diagnostics.Report("display-name", "Could not read primary name for " + id, exception);
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(definition.DisplayNameString))
                    return definition.DisplayNameString;
            }
            catch (Exception exception)
            {
                diagnostics.Report("display-name-fallback", "Could not read fallback name for " + id, exception);
            }
            return !string.IsNullOrWhiteSpace(id.SubtypeName) ? id.SubtypeName : id.ToString();
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

        private static BrowseCategory GetPhysicalBrowseCategory(MyPhysicalItemDefinition definition)
        {
            if (definition is MyComponentDefinition) return BrowseCategory.Components;
            if (definition.IsOre) return BrowseCategory.Ores;
            if (definition.IsIngot) return BrowseCategory.Ingots;
            if (definition is MyAmmoMagazineDefinition) return BrowseCategory.Ammo;
            if (definition is MyOxygenContainerDefinition) return BrowseCategory.GasBottles;
            if (definition is MyConsumableItemDefinition) return BrowseCategory.Consumables;
            if (definition is MyToolItemDefinition || definition is MyWeaponItemDefinition)
                return BrowseCategory.ToolsAndWeapons;
            return BrowseCategory.Items;
        }

        #endregion
    }
}
