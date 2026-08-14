using System;
using System.Collections.Generic;
using VRage.Game;

namespace SEpedia.Core
{
    internal enum TriStateFilter
    {
        Either = 0,
        Yes = 1,
        No = 2
    }

    internal sealed class CatalogEntry
    {
        public DefinitionDocument Definition { get; private set; }
        public PlanetSnapshot Planet { get; private set; }
        public BrowseCategory Category { get; private set; }
        public string DisplayName { get; private set; }
        public string StableKey { get; private set; }
        public string ListDetail { get; private set; }
        public int CelestialSortOrder { get; private set; }

        public bool IsSpawnedPlanet
        {
            get { return Planet != null; }
        }

        public DefinitionOrigin Origin
        {
            get { return Definition != null ? Definition.Origin : Planet.Origin; }
        }

        public bool IsEnabled
        {
            get { return Definition != null ? Definition.IsEnabled : Planet.IsEnabled; }
        }

        public bool IsPublic
        {
            get { return Definition != null ? Definition.IsPublic : Planet.IsPublic; }
        }

        public bool IsAvailableInSurvival
        {
            get
            {
                if (Definition != null && Definition.Recipe != null &&
                    Definition.BrowseCategory == BrowseCategory.Recipes)
                    return Definition.Recipe.IsProductionMenuReachable;
                return Definition != null
                    ? Definition.IsAvailableInSurvival
                    : Planet.IsAvailableInSurvival;
            }
        }

        public CatalogEntry(DefinitionDocument definition, int celestialSortOrder, string listDetail = null)
        {
            Definition = definition;
            Category = definition.BrowseCategory;
            DisplayName = definition.DisplayName;
            StableKey = "definition:" + definition.Id;
            ListDetail = listDetail ?? string.Empty;
            CelestialSortOrder = celestialSortOrder;
        }

        public CatalogEntry(PlanetSnapshot planet)
        {
            Planet = planet;
            Category = BrowseCategory.Celestial;
            DisplayName = planet.DisplayName;
            StableKey = "planet:" + planet.EntityId;
            ListDetail = string.Empty;
            CelestialSortOrder = 0;
        }
    }

    internal sealed class CatalogFilter
    {
        public BrowseCategory Category { get; set; }
        public string SearchText { get; set; }
        public TriStateFilter EnabledState { get; set; }
        public TriStateFilter PublicState { get; set; }
        public TriStateFilter SurvivalState { get; set; }
        public TriStateFilter BuildMenuState { get; set; }
        public HashSet<string> SelectedSourceKeys { get; private set; }
        public HashSet<MyCubeSize> SelectedGridSizes { get; private set; }
        public HashSet<string> SelectedBlockTypes { get; private set; }

        public CatalogFilter(bool survivalMode)
        {
            SelectedSourceKeys = new HashSet<string>(StringComparer.Ordinal);
            SelectedGridSizes = new HashSet<MyCubeSize>();
            SelectedBlockTypes = new HashSet<string>(StringComparer.Ordinal);
            Category = BrowseCategory.Components;
            SearchText = string.Empty;
            ResetAdvanced(survivalMode);
        }

        public void ResetAdvanced(bool survivalMode)
        {
            EnabledState = TriStateFilter.Yes;
            PublicState = TriStateFilter.Yes;
            SurvivalState = survivalMode ? TriStateFilter.Yes : TriStateFilter.Either;
            BuildMenuState = TriStateFilter.Yes;
            SelectedSourceKeys.Clear();
            SelectedGridSizes.Clear();
            SelectedGridSizes.Add(MyCubeSize.Small);
            SelectedGridSizes.Add(MyCubeSize.Large);
            SelectedBlockTypes.Clear();
        }

        public void NormalizeForCategory()
        {
            if (Category != BrowseCategory.Blocks)
            {
                SelectedBlockTypes.Clear();
                SelectedGridSizes.Clear();
                return;
            }

            if (SelectedGridSizes.Count == 0)
            {
                SelectedGridSizes.Add(MyCubeSize.Small);
                SelectedGridSizes.Add(MyCubeSize.Large);
            }
        }

        public bool ReconcileAvailableFacets(
            IReadOnlyList<FacetCount> sources,
            IReadOnlyList<FacetCount> blockTypes)
        {
            bool changed = RemoveUnavailableSelections(SelectedSourceKeys, sources);
            changed |= RemoveUnavailableSelections(SelectedBlockTypes, blockTypes);
            return changed;
        }

        private static bool RemoveUnavailableSelections(
            HashSet<string> selected,
            IReadOnlyList<FacetCount> available)
        {
            if (selected.Count == 0)
                return false;

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < available.Count; index++)
                keys.Add(available[index].Key);
            int originalCount = selected.Count;
            selected.RemoveWhere(delegate(string key) { return !keys.Contains(key); });
            return selected.Count != originalCount;
        }
    }

    internal sealed class FacetCount
    {
        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public int Count { get; private set; }

        public FacetCount(string key, string displayName, int count)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Count = count;
        }
    }

    internal sealed class CatalogResult
    {
        public IReadOnlyList<CatalogEntry> Items { get; private set; }
        public int TotalCount { get; private set; }
        public IReadOnlyList<FacetCount> Sources { get; private set; }
        public IReadOnlyList<FacetCount> BlockTypes { get; private set; }

        public CatalogResult(
            IList<CatalogEntry> items,
            int totalCount,
            IList<FacetCount> sources,
            IList<FacetCount> blockTypes)
        {
            Items = new List<CatalogEntry>(items).AsReadOnly();
            TotalCount = totalCount;
            Sources = new List<FacetCount>(sources).AsReadOnly();
            BlockTypes = new List<FacetCount>(blockTypes).AsReadOnly();
        }
    }
}
