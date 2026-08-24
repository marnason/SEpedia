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
        #region State

        private readonly Dictionary<string, FacetValue> facets;

        public DefinitionDocument Definition { get; private set; }
        public PlanetSnapshot Planet { get; private set; }
        public string CategoryKey { get; private set; }
        public string DisplayName { get; private set; }
        public string StableKey { get; private set; }
        public string CelestialKindKey { get; private set; }
        public string CelestialKindDisplayName { get; private set; }

        public bool IsSpawnedPlanet { get { return Planet != null; } }
        public DefinitionOrigin Origin { get { return Definition != null ? Definition.Origin : Planet.Origin; } }
        public bool IsEnabled { get { return Definition != null ? Definition.IsEnabled : Planet.IsEnabled; } }
        public bool IsPublic { get { return Definition != null ? Definition.IsPublic : Planet.IsPublic; } }

        public bool IsAvailableInSurvival
        {
            get
            {
                if (Definition != null && Definition.Recipe != null &&
                    Definition.CategoryKey == CatalogCategoryKeys.Recipes)
                    return Definition.Recipe.IsProductionMenuReachable;
                return Definition != null ? Definition.IsAvailableInSurvival : Planet.IsAvailableInSurvival;
            }
        }

        #endregion

        #region Construction

        public CatalogEntry(DefinitionDocument definition)
            : this(definition, GetDefinitionCelestialKindKey(definition), GetDefinitionCelestialKindDisplayName(definition))
        {
        }

        public CatalogEntry(DefinitionDocument definition, string celestialKindKey, string celestialKindDisplayName)
        {
            facets = new Dictionary<string, FacetValue>(StringComparer.Ordinal);
            Definition = definition;
            CategoryKey = definition.CategoryKey;
            StableKey = "definition:" + definition.Id;
            if (CategoryKey == CatalogCategoryKeys.Celestial)
            {
                CelestialKindKey = celestialKindKey ?? string.Empty;
                CelestialKindDisplayName = celestialKindDisplayName ?? string.Empty;
                AddFacet(CatalogFacetKeys.CelestialKind, CelestialKindKey, CelestialKindDisplayName);
                DisplayName = definition.UiDisplayName + " (" + CelestialKindDisplayName.ToLowerInvariant() + ")";
            }
            else
            {
                CelestialKindKey = string.Empty;
                CelestialKindDisplayName = string.Empty;
                DisplayName = definition.UiDisplayName;
            }

            if (definition.CubeBlock != null)
                AddFacet(CatalogFacetKeys.BlockType, definition.RuntimeTypeName, CatalogText.GetFriendlyRuntimeType(definition.RuntimeTypeName));
        }

        public CatalogEntry(PlanetSnapshot planet)
        {
            facets = new Dictionary<string, FacetValue>(StringComparer.Ordinal);
            Planet = planet;
            CategoryKey = CatalogCategoryKeys.Celestial;
            DisplayName = planet.DisplayName + " (spawned)";
            StableKey = "planet:" + planet.EntityId;
            CelestialKindKey = "spawned";
            CelestialKindDisplayName = "Spawned";
            AddFacet(CatalogFacetKeys.CelestialKind, CelestialKindKey, CelestialKindDisplayName);
        }

        private void AddFacet(string facetKey, string key, string displayName)
        {
            facets[facetKey] = new FacetValue(key, displayName);
        }

        public bool TryGetFacet(string facetKey, out FacetValue value)
        {
            return facets.TryGetValue(facetKey, out value);
        }

        public IEnumerable<FacetValue> GetFacetValues()
        {
            return facets.Values;
        }

        private static string GetDefinitionCelestialKindKey(DefinitionDocument definition)
        {
            if (definition.PlanetGenerator != null) return "generator-planet";
            if (definition.AsteroidGenerator != null) return "generator-asteroid";
            return definition.CategoryKey == CatalogCategoryKeys.Celestial ? "generator" : string.Empty;
        }

        private static string GetDefinitionCelestialKindDisplayName(DefinitionDocument definition)
        {
            if (definition.PlanetGenerator != null) return "Generator - Planet";
            if (definition.AsteroidGenerator != null) return "Generator - Asteroid";
            return definition.CategoryKey == CatalogCategoryKeys.Celestial ? "Generator" : string.Empty;
        }

        #endregion
    }

    internal sealed class FacetValue
    {
        public string Key { get; private set; }
        public string DisplayName { get; private set; }

        public FacetValue(string key, string displayName)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }
    }

    internal sealed class CatalogVisibilityFilter
    {
        private TriStateFilter defaultSurvivalState;

        public TriStateFilter EnabledState { get; set; }
        public TriStateFilter PublicState { get; set; }
        public TriStateFilter SurvivalState { get; set; }
        public HashSet<string> SelectedSourceKeys { get; private set; }

        public CatalogVisibilityFilter()
        {
            SelectedSourceKeys = new HashSet<string>(StringComparer.Ordinal);
        }

        public void Reset(bool survivalMode)
        {
            defaultSurvivalState = survivalMode ? TriStateFilter.Yes : TriStateFilter.Either;
            EnabledState = TriStateFilter.Yes;
            PublicState = TriStateFilter.Yes;
            SurvivalState = defaultSurvivalState;
            SelectedSourceKeys.Clear();
        }

        public int GetActiveFilterCount()
        {
            int count = 0;
            if (EnabledState != TriStateFilter.Yes) count++;
            if (PublicState != TriStateFilter.Yes) count++;
            if (SurvivalState != defaultSurvivalState) count++;
            if (SelectedSourceKeys.Count > 0) count++;
            return count;
        }
    }

    internal sealed class CatalogFilter
    {
        #region State

        private readonly Dictionary<string, HashSet<string>> selectedFacetValues;

        public CatalogSchema Schema { get; private set; }
        public CatalogVisibilityFilter Visibility { get; private set; }
        public CatalogEntryVisibility EntryVisibility { get; private set; }
        public string CategoryKey { get; set; }
        public string SearchText { get; set; }
        public TriStateFilter BuildMenuState { get; set; }
        public HashSet<MyCubeSize> SelectedGridSizes { get; private set; }

        #endregion

        #region Construction and Reset

        public CatalogFilter(CatalogSchema schema, bool survivalMode)
        {
            Schema = schema;
            Visibility = new CatalogVisibilityFilter();
            EntryVisibility = new CatalogEntryVisibility();
            SelectedGridSizes = new HashSet<MyCubeSize>();
            selectedFacetValues = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            CategoryKey = schema.FirstCategory.Key;
            SearchText = string.Empty;
            ResetAdvanced(survivalMode);
        }

        public HashSet<string> GetSelectedFacetValues(string facetKey)
        {
            HashSet<string> selected;
            if (!selectedFacetValues.TryGetValue(facetKey, out selected))
            {
                selected = new HashSet<string>(StringComparer.Ordinal);
                selectedFacetValues.Add(facetKey, selected);
            }
            return selected;
        }

        public void ResetAdvanced(bool survivalMode)
        {
            Visibility.Reset(survivalMode);
            BuildMenuState = TriStateFilter.Yes;
            SelectedGridSizes.Clear();
            SelectedGridSizes.Add(MyCubeSize.Small);
            SelectedGridSizes.Add(MyCubeSize.Large);
            foreach (KeyValuePair<string, HashSet<string>> pair in selectedFacetValues)
                pair.Value.Clear();
        }

        public int GetActiveAdvancedFilterCount()
        {
            int count = Visibility.GetActiveFilterCount();
            if (CategoryKey == CatalogCategoryKeys.Blocks)
            {
                if (BuildMenuState != TriStateFilter.Yes) count++;
                if (SelectedGridSizes.Count != 2 ||
                    !SelectedGridSizes.Contains(MyCubeSize.Small) ||
                    !SelectedGridSizes.Contains(MyCubeSize.Large)) count++;
            }

            CatalogCategoryDefinition category = Schema.GetCategory(CategoryKey);
            if (category != null)
            {
                for (int index = 0; index < category.Facets.Count; index++)
                {
                    if (GetSelectedFacetValues(category.Facets[index].Key).Count > 0) count++;
                }
            }
            return count;
        }

        #endregion

        #region Category and Facet Reconciliation

        public void NormalizeForCategory()
        {
            if (CategoryKey != CatalogCategoryKeys.Blocks)
                SelectedGridSizes.Clear();
            else if (SelectedGridSizes.Count == 0)
            {
                SelectedGridSizes.Add(MyCubeSize.Small);
                SelectedGridSizes.Add(MyCubeSize.Large);
            }

            CatalogCategoryDefinition category = Schema.GetCategory(CategoryKey);
            foreach (KeyValuePair<string, HashSet<string>> pair in selectedFacetValues)
            {
                if (category == null || !category.HasFacet(pair.Key)) pair.Value.Clear();
            }
        }

        public bool ReconcileAvailableFacets(
            IReadOnlyList<FacetCount> sources,
            IDictionary<string, IReadOnlyList<FacetCount>> facets)
        {
            bool changed = RemoveUnavailableSelections(Visibility.SelectedSourceKeys, sources);
            foreach (KeyValuePair<string, HashSet<string>> pair in selectedFacetValues)
            {
                IReadOnlyList<FacetCount> available;
                if (facets.TryGetValue(pair.Key, out available))
                    changed |= RemoveUnavailableSelections(pair.Value, available);
            }
            return changed;
        }

        private static bool RemoveUnavailableSelections(HashSet<string> selected, IReadOnlyList<FacetCount> available)
        {
            if (selected.Count == 0) return false;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < available.Count; index++) keys.Add(available[index].Key);
            int originalCount = selected.Count;
            selected.RemoveWhere(delegate(string key) { return !keys.Contains(key); });
            return selected.Count != originalCount;
        }

        #endregion
    }

    internal sealed class CatalogEntryVisibility
    {
        public bool IsCommonlyVisible(CatalogEntry entry, CatalogVisibilityFilter filter)
        {
            return entry != null && MatchesCommonFlags(entry, filter) && MatchesSource(entry, filter);
        }

        public bool MatchesCommonFlags(CatalogEntry entry, CatalogVisibilityFilter filter)
        {
            return entry != null &&
                MatchesTriState(entry.IsEnabled, filter.EnabledState) &&
                MatchesTriState(entry.IsPublic, filter.PublicState) &&
                MatchesTriState(entry.IsAvailableInSurvival, filter.SurvivalState);
        }

        public bool MatchesSource(CatalogEntry entry, CatalogVisibilityFilter filter)
        {
            return entry != null && (filter.SelectedSourceKeys.Count == 0 ||
                filter.SelectedSourceKeys.Contains(entry.Origin.SourceKey));
        }

        public bool IsListVisible(CatalogEntry entry, CatalogFilter filter)
        {
            if (entry.CategoryKey != filter.CategoryKey || !IsCommonlyVisible(entry, filter.Visibility)) return false;
            if (filter.CategoryKey == CatalogCategoryKeys.Blocks)
            {
                CubeBlockData block = entry.Definition != null ? entry.Definition.CubeBlock : null;
                if (block == null || !MatchesTriState(block.IsBuildMenuReachable, filter.BuildMenuState) ||
                    !filter.SelectedGridSizes.Contains(block.CubeSize)) return false;
            }

            CatalogCategoryDefinition category = filter.Schema.GetCategory(filter.CategoryKey);
            if (category == null) return true;
            for (int index = 0; index < category.Facets.Count; index++)
            {
                string facetKey = category.Facets[index].Key;
                HashSet<string> selected = filter.GetSelectedFacetValues(facetKey);
                FacetValue value;
                if (selected.Count > 0 && (!entry.TryGetFacet(facetKey, out value) || !selected.Contains(value.Key)))
                    return false;
            }
            return true;
        }

        public static bool MatchesTriState(bool value, TriStateFilter filter)
        {
            return filter == TriStateFilter.Either ||
                (filter == TriStateFilter.Yes && value) ||
                (filter == TriStateFilter.No && !value);
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
        private static readonly IReadOnlyList<FacetCount> EmptyFacets = new List<FacetCount>().AsReadOnly();
        private readonly Dictionary<string, IReadOnlyList<FacetCount>> facets;

        public IReadOnlyList<CatalogEntry> Items { get; private set; }
        public int TotalCount { get; private set; }
        public IReadOnlyList<FacetCount> Sources { get; private set; }
        public IDictionary<string, IReadOnlyList<FacetCount>> Facets { get { return facets; } }

        public CatalogResult(IList<CatalogEntry> items, int totalCount, IList<FacetCount> sources,
            IDictionary<string, IList<FacetCount>> facetValues)
        {
            Items = new List<CatalogEntry>(items).AsReadOnly();
            TotalCount = totalCount;
            Sources = new List<FacetCount>(sources).AsReadOnly();
            facets = new Dictionary<string, IReadOnlyList<FacetCount>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IList<FacetCount>> pair in facetValues)
                facets[pair.Key] = new List<FacetCount>(pair.Value).AsReadOnly();
        }

        public IReadOnlyList<FacetCount> GetFacets(string facetKey)
        {
            IReadOnlyList<FacetCount> values;
            return facets.TryGetValue(facetKey, out values) ? values : EmptyFacets;
        }
    }
}
