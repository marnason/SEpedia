using System;
using System.Collections.Generic;

namespace SEpedia.Core
{
    internal delegate bool CatalogCategoryAvailability(CatalogIndex catalog, bool survivalMode);

    internal static class CatalogCategoryKeys
    {
        public const string Components = "components";
        public const string Ores = "ores";
        public const string Ingots = "ingots";
        public const string Ammo = "ammo";
        public const string ToolsAndWeapons = "tools-weapons";
        public const string Consumables = "consumables";
        public const string Items = "items";
        public const string Blocks = "blocks";
        public const string Recipes = "recipes";
        public const string Celestial = "celestial";
    }

    internal static class CatalogFacetKeys
    {
        public const string BlockType = "block-type";
        public const string CelestialKind = "celestial-kind";
    }

    internal sealed class CatalogFacetDefinition
    {
        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public string AllDisplayName { get; private set; }
        public int Order { get; private set; }
        public bool ShowKeyTooltips { get; private set; }

        public CatalogFacetDefinition(
            string key,
            string displayName,
            string allDisplayName,
            int order,
            bool showKeyTooltips)
        {
            Key = key;
            DisplayName = displayName;
            AllDisplayName = allDisplayName;
            Order = order;
            ShowKeyTooltips = showKeyTooltips;
        }
    }

    internal sealed class CatalogCategoryDefinition
    {
        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public int Order { get; private set; }
        public IReadOnlyList<CatalogFacetDefinition> Facets { get; private set; }
        public CatalogCategoryAvailability Availability { get; private set; }

        public CatalogCategoryDefinition(
            string key,
            string displayName,
            int order,
            params CatalogFacetDefinition[] facets)
            : this(key, displayName, order, null, facets)
        {
        }

        public CatalogCategoryDefinition(
            string key,
            string displayName,
            int order,
            CatalogCategoryAvailability availability,
            params CatalogFacetDefinition[] facets)
        {
            Key = key;
            DisplayName = displayName;
            Order = order;
            Availability = availability ?? DefaultAvailability;
            var values = new List<CatalogFacetDefinition>(facets ?? new CatalogFacetDefinition[0]);
            values.Sort(delegate(CatalogFacetDefinition left, CatalogFacetDefinition right)
            {
                return left.Order.CompareTo(right.Order);
            });
            Facets = values.AsReadOnly();
        }

        public bool HasFacet(string facetKey)
        {
            for (int index = 0; index < Facets.Count; index++)
            {
                if (Facets[index].Key == facetKey)
                    return true;
            }
            return false;
        }

        public bool IsAvailable(CatalogIndex catalog, bool survivalMode)
        {
            return Availability(catalog, survivalMode);
        }

        private bool DefaultAvailability(CatalogIndex catalog, bool survivalMode)
        {
            return catalog.HasMultipleDefaultEntries(Key, survivalMode);
        }
    }

    internal sealed class CatalogSchema
    {
        private readonly Dictionary<string, CatalogCategoryDefinition> byKey;

        public IReadOnlyList<CatalogCategoryDefinition> Categories { get; private set; }

        public CatalogCategoryDefinition FirstCategory
        {
            get { return Categories[0]; }
        }

        public CatalogSchema(IEnumerable<CatalogCategoryDefinition> categories)
        {
            var values = new List<CatalogCategoryDefinition>(categories);
            values.Sort(delegate(CatalogCategoryDefinition left, CatalogCategoryDefinition right)
            {
                return left.Order.CompareTo(right.Order);
            });
            Categories = values.AsReadOnly();
            byKey = new Dictionary<string, CatalogCategoryDefinition>(StringComparer.Ordinal);
            for (int index = 0; index < values.Count; index++)
                byKey.Add(values[index].Key, values[index]);
        }

        public CatalogCategoryDefinition GetCategory(string key)
        {
            CatalogCategoryDefinition category;
            return key != null && byKey.TryGetValue(key, out category) ? category : null;
        }

        public static CatalogSchema CreateBuiltIn()
        {
            var blockType = new CatalogFacetDefinition(
                CatalogFacetKeys.BlockType, "Runtime block type", "All block types", 10, true);
            var celestialKind = new CatalogFacetDefinition(
                CatalogFacetKeys.CelestialKind, "Celestial type", "All celestial types", 10, false);
            return new CatalogSchema(new[]
            {
                new CatalogCategoryDefinition(CatalogCategoryKeys.Components, "Components", 10),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Ores, "Ores", 20),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Ingots, "Ingots", 30),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Ammo, "Ammo", 40),
                new CatalogCategoryDefinition(CatalogCategoryKeys.ToolsAndWeapons, "Tools & Weapons", 50),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Consumables, "Consumables", 60),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Items, "Items", 70),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Blocks, "Blocks", 80, blockType),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Recipes, "Recipes", 90),
                new CatalogCategoryDefinition(CatalogCategoryKeys.Celestial, "Celestial", 100, celestialKind)
            });
        }
    }
}
