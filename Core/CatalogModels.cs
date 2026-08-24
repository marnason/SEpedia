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

        public DefinitionDocument Definition { get; private set; }
        public PlanetSnapshot Planet { get; private set; }
        public BrowseCategory Category { get; private set; }
        public string DisplayName { get; private set; }
        public string StableKey { get; private set; }
        public string CelestialKindKey { get; private set; }
        public string CelestialKindDisplayName { get; private set; }

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

        #endregion

        #region Construction

        public CatalogEntry(DefinitionDocument definition)
            : this(
                definition,
                GetDefinitionCelestialKindKey(definition),
                GetDefinitionCelestialKindDisplayName(definition))
        {
        }

        public CatalogEntry(
            DefinitionDocument definition,
            string celestialKindKey,
            string celestialKindDisplayName)
        {
            Definition = definition;
            Category = definition.BrowseCategory;
            StableKey = "definition:" + definition.Id;
            if (Category == BrowseCategory.Celestial)
            {
                CelestialKindKey = celestialKindKey ?? string.Empty;
                CelestialKindDisplayName = celestialKindDisplayName ?? string.Empty;
                DisplayName = definition.UiDisplayName + " (" +
                    CelestialKindDisplayName.ToLowerInvariant() + ")";
            }
            else
            {
                CelestialKindKey = string.Empty;
                CelestialKindDisplayName = string.Empty;
                DisplayName = definition.UiDisplayName;
            }
        }

        public CatalogEntry(PlanetSnapshot planet)
        {
            Planet = planet;
            Category = BrowseCategory.Celestial;
            DisplayName = planet.DisplayName + " (spawned)";
            StableKey = "planet:" + planet.EntityId;
            CelestialKindKey = "spawned";
            CelestialKindDisplayName = "Spawned";
        }

        private static string GetDefinitionCelestialKindKey(DefinitionDocument definition)
        {
            if (definition.PlanetGenerator != null)
                return "generator-planet";
            if (definition.AsteroidGenerator != null)
                return "generator-asteroid";
            return definition.BrowseCategory == BrowseCategory.Celestial
                ? "generator"
                : string.Empty;
        }

        private static string GetDefinitionCelestialKindDisplayName(DefinitionDocument definition)
        {
            if (definition.PlanetGenerator != null)
                return "Generator - Planet";
            if (definition.AsteroidGenerator != null)
                return "Generator - Asteroid";
            return definition.BrowseCategory == BrowseCategory.Celestial
                ? "Generator"
                : string.Empty;
        }

        #endregion
    }

    internal sealed class CatalogFilter
    {
        #region State

        public BrowseCategory Category { get; set; }
        public string SearchText { get; set; }
        public TriStateFilter EnabledState { get; set; }
        public TriStateFilter PublicState { get; set; }
        public TriStateFilter SurvivalState { get; set; }
        public TriStateFilter BuildMenuState { get; set; }
        public HashSet<string> SelectedSourceKeys { get; private set; }
        public HashSet<MyCubeSize> SelectedGridSizes { get; private set; }
        public HashSet<string> SelectedBlockTypes { get; private set; }
        public HashSet<string> SelectedCelestialKinds { get; private set; }
        private TriStateFilter defaultSurvivalState;

        #endregion

        #region Construction and Reset

        public CatalogFilter(bool survivalMode)
        {
            SelectedSourceKeys = new HashSet<string>(StringComparer.Ordinal);
            SelectedGridSizes = new HashSet<MyCubeSize>();
            SelectedBlockTypes = new HashSet<string>(StringComparer.Ordinal);
            SelectedCelestialKinds = new HashSet<string>(StringComparer.Ordinal);
            Category = BrowseCategory.Components;
            SearchText = string.Empty;
            ResetAdvanced(survivalMode);
        }

        public void ResetAdvanced(bool survivalMode)
        {
            defaultSurvivalState = survivalMode ? TriStateFilter.Yes : TriStateFilter.Either;
            EnabledState = TriStateFilter.Yes;
            PublicState = TriStateFilter.Yes;
            SurvivalState = defaultSurvivalState;
            BuildMenuState = TriStateFilter.Yes;
            SelectedSourceKeys.Clear();
            SelectedGridSizes.Clear();
            SelectedGridSizes.Add(MyCubeSize.Small);
            SelectedGridSizes.Add(MyCubeSize.Large);
            SelectedBlockTypes.Clear();
            SelectedCelestialKinds.Clear();
        }

        public int GetActiveAdvancedFilterCount()
        {
            int count = 0;
            if (EnabledState != TriStateFilter.Yes)
                count++;
            if (PublicState != TriStateFilter.Yes)
                count++;
            if (SurvivalState != defaultSurvivalState)
                count++;
            if (SelectedSourceKeys.Count > 0)
                count++;

            if (Category == BrowseCategory.Blocks)
            {
                if (BuildMenuState != TriStateFilter.Yes)
                    count++;
                if (SelectedGridSizes.Count != 2 ||
                    !SelectedGridSizes.Contains(MyCubeSize.Small) ||
                    !SelectedGridSizes.Contains(MyCubeSize.Large))
                    count++;
                if (SelectedBlockTypes.Count > 0)
                    count++;
            }
            if (Category == BrowseCategory.Celestial && SelectedCelestialKinds.Count > 0)
                count++;
            return count;
        }

        #endregion

        #region Category and Facet Reconciliation

        public void NormalizeForCategory()
        {
            if (Category != BrowseCategory.Blocks)
            {
                SelectedBlockTypes.Clear();
                SelectedGridSizes.Clear();
            }
            else if (SelectedGridSizes.Count == 0)
            {
                SelectedGridSizes.Add(MyCubeSize.Small);
                SelectedGridSizes.Add(MyCubeSize.Large);
            }

            if (Category != BrowseCategory.Celestial)
                SelectedCelestialKinds.Clear();
        }

        public bool ReconcileAvailableFacets(
            IReadOnlyList<FacetCount> sources,
            IReadOnlyList<FacetCount> blockTypes,
            IReadOnlyList<FacetCount> celestialKinds)
        {
            bool changed = RemoveUnavailableSelections(SelectedSourceKeys, sources);
            changed |= RemoveUnavailableSelections(SelectedBlockTypes, blockTypes);
            changed |= RemoveUnavailableSelections(SelectedCelestialKinds, celestialKinds);
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

        #endregion
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
        public IReadOnlyList<FacetCount> CelestialKinds { get; private set; }

        public CatalogResult(
            IList<CatalogEntry> items,
            int totalCount,
            IList<FacetCount> sources,
            IList<FacetCount> blockTypes,
            IList<FacetCount> celestialKinds)
        {
            Items = new List<CatalogEntry>(items).AsReadOnly();
            TotalCount = totalCount;
            Sources = new List<FacetCount>(sources).AsReadOnly();
            BlockTypes = new List<FacetCount>(blockTypes).AsReadOnly();
            CelestialKinds = new List<FacetCount>(celestialKinds).AsReadOnly();
        }
    }
}
