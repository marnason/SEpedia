using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;

namespace SEpedia.Core
{
    internal static class CatalogText
    {
        public static string GetCategoryName(BrowseCategory category)
        {
            switch (category)
            {
                case BrowseCategory.Components: return "Components";
                case BrowseCategory.Ores: return "Ores";
                case BrowseCategory.Ingots: return "Ingots";
                case BrowseCategory.Ammo: return "Ammo";
                case BrowseCategory.ToolsAndWeapons: return "Tools & Weapons";
                case BrowseCategory.Consumables: return "Consumables";
                case BrowseCategory.GasBottles: return "Gas Bottles";
                case BrowseCategory.Items: return "Items";
                case BrowseCategory.Blocks: return "Blocks";
                case BrowseCategory.Recipes: return "Recipes";
                case BrowseCategory.Celestial: return "Celestial";
                default: return "Entries";
            }
        }

        public static string GetEntryLabel(CatalogEntry entry)
        {
            if (entry.IsSpawnedPlanet)
                return "Spawned planet";
            if (entry.Definition.AsteroidGenerator != null)
                return "Asteroid generator";
            if (entry.Definition.PlanetGenerator != null)
                return "Planet definition";
            if (entry.Category == BrowseCategory.Recipes)
                return string.IsNullOrWhiteSpace(entry.ListDetail) ? "Recipe" : entry.ListDetail;
            switch (entry.Category)
            {
                case BrowseCategory.Components: return "Component";
                case BrowseCategory.Ores: return "Ore";
                case BrowseCategory.Ingots: return "Ingot";
                case BrowseCategory.Ammo: return "Ammo";
                case BrowseCategory.ToolsAndWeapons: return "Tool / weapon";
                case BrowseCategory.Consumables: return "Consumable";
                case BrowseCategory.GasBottles: return "Gas bottle";
                case BrowseCategory.Items: return "Item";
                case BrowseCategory.Blocks: return "Block";
                default: return "Entry";
            }
        }

        public static string GetFriendlyRuntimeType(string runtimeType)
        {
            if (string.IsNullOrWhiteSpace(runtimeType))
                return "Unknown";

            int separator = runtimeType.LastIndexOf('.');
            string name = separator >= 0 ? runtimeType.Substring(separator + 1) : runtimeType;
            if (name.StartsWith("My", StringComparison.Ordinal) && name.Length > 2)
                name = name.Substring(2);
            if (name.EndsWith("Definition", StringComparison.Ordinal) && name.Length > 10)
                name = name.Substring(0, name.Length - 10);

            var builder = new StringBuilder(name.Length + 8);
            for (int index = 0; index < name.Length; index++)
            {
                char current = name[index];
                if (index > 0 && char.IsUpper(current) && !char.IsUpper(name[index - 1]))
                    builder.Append(' ');
                builder.Append(current);
            }
            return builder.ToString();
        }

        public static string BuildRecipeSummary(RecipeDocument recipe, DefinitionIndex definitions)
        {
            if (recipe == null)
                return "Recipe";
            return JoinItemNames(recipe.Prerequisites, definitions, 2) + " → " +
                JoinItemNames(recipe.Results, definitions, 2);
        }

        public static string BuildRecipeSearchBlob(RecipeDocument recipe, DefinitionIndex definitions)
        {
            var builder = new StringBuilder();
            AppendDefinitions(builder, recipe.Prerequisites, definitions);
            AppendDefinitions(builder, recipe.Results, definitions);
            for (int index = 0; index < recipe.ProductionBlocks.Count; index++)
                AppendDefinition(builder, recipe.ProductionBlocks[index], definitions);
            return builder.ToString();
        }

        private static string JoinItemNames(
            IReadOnlyList<DefinitionAmount> amounts,
            DefinitionIndex definitions,
            int limit)
        {
            if (amounts.Count == 0)
                return "None";

            var builder = new StringBuilder();
            int count = Math.Min(amounts.Count, limit);
            for (int index = 0; index < count; index++)
            {
                if (builder.Length > 0)
                    builder.Append(" + ");
                DefinitionDocument item;
                builder.Append(definitions.TryGet(amounts[index].DefinitionId, out item)
                    ? item.DisplayName
                    : amounts[index].DefinitionId.SubtypeName);
            }
            if (amounts.Count > limit)
                builder.Append(" + …");
            return builder.ToString();
        }

        private static void AppendDefinitions(
            StringBuilder builder,
            IReadOnlyList<DefinitionAmount> amounts,
            DefinitionIndex definitions)
        {
            for (int index = 0; index < amounts.Count; index++)
                AppendDefinition(builder, amounts[index].DefinitionId, definitions);
        }

        private static void AppendDefinition(
            StringBuilder builder,
            MyDefinitionId id,
            DefinitionIndex definitions)
        {
            DefinitionDocument definition;
            if (definitions.TryGet(id, out definition))
                builder.Append(' ').Append(definition.DisplayName).Append(' ').Append(definition.SubtypeName);
            builder.Append(' ').Append(id);
        }
    }
}
