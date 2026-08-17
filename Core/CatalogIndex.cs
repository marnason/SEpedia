using System;
using System.Collections.Generic;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class CatalogIndex
    {
        #region Search State

        private sealed class SearchableEntry
        {
            public CatalogEntry Entry;
            public string Name;
            public string Subtype;
            public string Blob;
        }

        private struct ScoredEntry
        {
            public CatalogEntry Entry;
            public int Score;
        }

        private static readonly IReadOnlyList<SearchableEntry> EmptyEntries = new List<SearchableEntry>().AsReadOnly();
        private readonly Dictionary<BrowseCategory, List<SearchableEntry>> entriesByCategory;

        #endregion

        #region Index Construction

        public CatalogIndex(DefinitionIndex definitions, IEnumerable<PlanetSnapshot> planets)
        {
            entriesByCategory = new Dictionary<BrowseCategory, List<SearchableEntry>>();

            for (int index = 0; index < definitions.All.Count; index++)
            {
                DefinitionDocument definition = definitions.All[index];
                if (definition.BrowseCategory == BrowseCategory.None)
                    continue;

                int celestialOrder = definition.AsteroidGenerator != null ? 1 : 2;
                Add(new CatalogEntry(definition, celestialOrder), definitions);
            }

            if (planets != null)
            {
                foreach (PlanetSnapshot planet in planets)
                    Add(new CatalogEntry(planet), definitions);
            }
        }

        private void Add(CatalogEntry entry, DefinitionIndex definitions)
        {
            string subtype = entry.Definition != null ? entry.Definition.SubtypeName : entry.Planet.EntityId.ToString();
            string id = entry.Definition != null ? entry.Definition.Id.ToString() : entry.Planet.EntityId.ToString();
            string runtimeType = entry.Definition != null ? entry.Definition.RuntimeTypeName : "Spawned planet";
            string category = CatalogText.GetCategoryName(entry.Category);
            string name = Normalize(entry.DisplayName);
            string normalizedSubtype = Normalize(subtype);
            string relationships = entry.Definition != null && entry.Definition.Recipe != null
                ? CatalogText.BuildRecipeSearchBlob(entry.Definition.Recipe, definitions)
                : string.Empty;

            var searchable = new SearchableEntry
            {
                Entry = entry,
                Name = name,
                Subtype = normalizedSubtype,
                Blob = string.Join(" ", new[]
                {
                    name,
                    entry.Definition != null ? Normalize(entry.Definition.AuthoredDisplayName) : string.Empty,
                    normalizedSubtype,
                    Normalize(id),
                    Normalize(category),
                    Normalize(runtimeType),
                    Normalize(entry.Origin.DisplayName),
                    Normalize(entry.Origin.ModId),
                    Normalize(entry.Origin.ServiceName),
                    Normalize(relationships)
                })
            };

            List<SearchableEntry> categoryEntries;
            if (!entriesByCategory.TryGetValue(entry.Category, out categoryEntries))
            {
                categoryEntries = new List<SearchableEntry>();
                entriesByCategory.Add(entry.Category, categoryEntries);
            }
            categoryEntries.Add(searchable);
        }

        #endregion

        #region Catalog Queries

        public CatalogResult Query(CatalogFilter filter, int offset, int limit)
        {
            return Query(filter, offset, limit, null);
        }

        public bool HasMultipleDefaultEntries(BrowseCategory category, bool survivalMode)
        {
            List<SearchableEntry> categoryEntries;
            if (!entriesByCategory.TryGetValue(category, out categoryEntries))
                return false;

            var defaultFilter = new CatalogFilter(survivalMode) { Category = category };
            int count = 0;
            for (int index = 0; index < categoryEntries.Count; index++)
            {
                if (MatchesAllFilters(categoryEntries[index].Entry, defaultFilter) && ++count > 1)
                    return true;
            }
            return false;
        }

        public CatalogResult Query(
            CatalogFilter filter,
            int offset,
            int limit,
            DefinitionDocument includedDefinition)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            List<FacetCount> sources;
            List<FacetCount> blockTypes;
            List<ScoredEntry> matches = FindMatches(
                filter,
                includedDefinition,
                out sources,
                out blockTypes);

            int first = Math.Min(Math.Max(0, offset), matches.Count);
            int count = Math.Min(Math.Max(0, limit), matches.Count - first);
            var result = new List<CatalogEntry>(count);
            for (int index = 0; index < count; index++)
                result.Add(matches[first + index].Entry);

            return new CatalogResult(result, matches.Count, sources, blockTypes);
        }

        public int FindDefinitionResultIndex(
            CatalogFilter filter,
            MyDefinitionId definitionId,
            DefinitionDocument includedDefinition,
            out int totalCount)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            List<FacetCount> sources;
            List<FacetCount> blockTypes;
            List<ScoredEntry> matches = FindMatches(
                filter,
                includedDefinition,
                out sources,
                out blockTypes);
            totalCount = matches.Count;
            for (int index = 0; index < matches.Count; index++)
            {
                DefinitionDocument definition = matches[index].Entry.Definition;
                if (definition != null && definition.Id == definitionId)
                    return index;
            }
            return -1;
        }

        private List<ScoredEntry> FindMatches(
            CatalogFilter filter,
            DefinitionDocument includedDefinition,
            out List<FacetCount> sources,
            out List<FacetCount> blockTypes)
        {
            string query = Normalize(filter.SearchText).Trim();
            string[] tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<SearchableEntry> categoryList;
            IReadOnlyList<SearchableEntry> categoryEntries = entriesByCategory.TryGetValue(filter.Category, out categoryList)
                ? categoryList
                : EmptyEntries;
            BuildFacets(categoryEntries, filter, query, tokens, out sources, out blockTypes);

            var matches = new List<ScoredEntry>();
            bool forcedDefinitionIncluded = false;

            for (int index = 0; index < categoryEntries.Count; index++)
            {
                SearchableEntry searchable = categoryEntries[index];
                bool isIncludedDefinition = includedDefinition != null &&
                    searchable.Entry.Definition != null &&
                    searchable.Entry.Definition.Id == includedDefinition.Id;
                bool matchesFilters = MatchesAllFilters(searchable.Entry, filter);
                int score = Score(searchable, query, tokens);
                bool forceInclude = isIncludedDefinition && (!matchesFilters || score < 0);
                if (!forceInclude && (!matchesFilters || score < 0))
                    continue;

                forcedDefinitionIncluded |= forceInclude;
                matches.Add(new ScoredEntry { Entry = searchable.Entry, Score = score });
            }

            if (forcedDefinitionIncluded)
                matches.Sort(CompareAlphabetically);
            else
                matches.Sort(CompareScored);
            return matches;
        }

        #endregion

        #region Facet Calculation

        private void BuildFacets(
            IReadOnlyList<SearchableEntry> categoryEntries,
            CatalogFilter filter,
            string query,
            string[] tokens,
            out List<FacetCount> sources,
            out List<FacetCount> blockTypes)
        {
            var sourceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var sourceNames = new Dictionary<string, string>(StringComparer.Ordinal);
            var blockTypeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var blockTypeNames = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int index = 0; index < categoryEntries.Count; index++)
            {
                SearchableEntry searchable = categoryEntries[index];
                CatalogEntry entry = searchable.Entry;
                if (!MatchesTriState(entry.IsEnabled, filter.EnabledState) ||
                    !MatchesTriState(entry.IsPublic, filter.PublicState) ||
                    !MatchesTriState(entry.IsAvailableInSurvival, filter.SurvivalState) ||
                    Score(searchable, query, tokens) < 0)
                    continue;

                if (MatchesBlockFilters(entry, filter) && MatchesBlockType(entry, filter))
                {
                    string sourceKey = entry.Origin.SourceKey;
                    AddFacet(sourceCounts, sourceNames, sourceKey, entry.Origin.DisplayName);
                }

                if (filter.Category == BrowseCategory.Blocks &&
                    MatchesSource(entry, filter) && MatchesBlockFilters(entry, filter))
                {
                    DefinitionDocument definition = entry.Definition;
                    if (definition != null && definition.CubeBlock != null)
                    {
                        string blockTypeKey = definition.RuntimeTypeName;
                        AddFacet(blockTypeCounts, blockTypeNames, blockTypeKey, CatalogText.GetFriendlyRuntimeType(blockTypeKey));
                    }
                }
            }

            sources = CreateFacets(sourceCounts, sourceNames);
            sources.Sort(CompareSourceFacet);
            blockTypes = CreateFacets(blockTypeCounts, blockTypeNames);
            blockTypes.Sort(CompareFacet);
        }

        private static void AddFacet(
            IDictionary<string, int> counts,
            IDictionary<string, string> displayNames,
            string key,
            string displayName)
        {
            int count;
            counts.TryGetValue(key, out count);
            counts[key] = count + 1;
            displayNames[key] = displayName;
        }

        private static List<FacetCount> CreateFacets(
            IDictionary<string, int> counts,
            IDictionary<string, string> displayNames)
        {
            var result = new List<FacetCount>(counts.Count);
            foreach (KeyValuePair<string, int> pair in counts)
                result.Add(new FacetCount(pair.Key, displayNames[pair.Key], pair.Value));
            return result;
        }

        #endregion

        #region Filter Matching

        private static bool MatchesAllFilters(CatalogEntry entry, CatalogFilter filter)
        {
            return MatchesCommonFilters(entry, filter) &&
                MatchesSource(entry, filter) &&
                MatchesBlockFilters(entry, filter) &&
                MatchesBlockType(entry, filter);
        }

        private static bool MatchesCommonFilters(CatalogEntry entry, CatalogFilter filter)
        {
            if (entry.Category != filter.Category ||
                !MatchesTriState(entry.IsEnabled, filter.EnabledState) ||
                !MatchesTriState(entry.IsPublic, filter.PublicState) ||
                !MatchesTriState(entry.IsAvailableInSurvival, filter.SurvivalState))
                return false;

            return true;
        }

        private static bool MatchesSource(CatalogEntry entry, CatalogFilter filter)
        {
            return filter.SelectedSourceKeys.Count == 0 ||
                filter.SelectedSourceKeys.Contains(entry.Origin.SourceKey);
        }

        private static bool MatchesBlockFilters(CatalogEntry entry, CatalogFilter filter)
        {
            if (filter.Category != BrowseCategory.Blocks)
                return true;

            CubeBlockData block = entry.Definition != null ? entry.Definition.CubeBlock : null;
            return block != null &&
                MatchesTriState(block.IsBuildMenuReachable, filter.BuildMenuState) &&
                filter.SelectedGridSizes.Contains(block.CubeSize);
        }

        private static bool MatchesBlockType(CatalogEntry entry, CatalogFilter filter)
        {
            return filter.Category != BrowseCategory.Blocks ||
                filter.SelectedBlockTypes.Count == 0 ||
                filter.SelectedBlockTypes.Contains(entry.Definition.RuntimeTypeName);
        }

        private static bool MatchesTriState(bool value, TriStateFilter filter)
        {
            return filter == TriStateFilter.Either ||
                (filter == TriStateFilter.Yes && value) ||
                (filter == TriStateFilter.No && !value);
        }

        #endregion

        #region Search Scoring and Ordering

        private static int Score(SearchableEntry entry, string query, string[] tokens)
        {
            if (tokens.Length == 0)
                return 0;

            for (int index = 0; index < tokens.Length; index++)
            {
                if (entry.Blob.IndexOf(tokens[index], StringComparison.Ordinal) < 0)
                    return -1;
            }

            int score = tokens.Length * 10;
            if (entry.Name == query)
                score += 1000;
            else if (entry.Subtype == query)
                score += 950;
            else if (entry.Name.StartsWith(query, StringComparison.Ordinal))
                score += 700;
            else if (entry.Subtype.StartsWith(query, StringComparison.Ordinal))
                score += 650;
            else if (entry.Name.IndexOf(query, StringComparison.Ordinal) >= 0)
                score += 400;
            else if (entry.Subtype.IndexOf(query, StringComparison.Ordinal) >= 0)
                score += 350;
            return score;
        }

        private static int CompareScored(ScoredEntry left, ScoredEntry right)
        {
            if (left.Entry.Category == BrowseCategory.Celestial && right.Entry.Category == BrowseCategory.Celestial)
            {
                int kind = left.Entry.CelestialSortOrder.CompareTo(right.Entry.CelestialSortOrder);
                if (kind != 0)
                    return kind;
            }

            int score = right.Score.CompareTo(left.Score);
            if (score != 0)
                return score;
            return CompareAlphabetically(left, right);
        }

        private static int CompareAlphabetically(ScoredEntry left, ScoredEntry right)
        {
            if (left.Entry.Category == BrowseCategory.Celestial && right.Entry.Category == BrowseCategory.Celestial)
            {
                int kind = left.Entry.CelestialSortOrder.CompareTo(right.Entry.CelestialSortOrder);
                if (kind != 0)
                    return kind;
            }

            int name = string.Compare(left.Entry.DisplayName, right.Entry.DisplayName, StringComparison.OrdinalIgnoreCase);
            return name != 0
                ? name
                : string.Compare(left.Entry.StableKey, right.Entry.StableKey, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareFacet(FacetCount left, FacetCount right)
        {
            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareSourceFacet(FacetCount left, FacetCount right)
        {
            bool leftVanilla = left.Key == "vanilla";
            bool rightVanilla = right.Key == "vanilla";
            if (leftVanilla != rightVanilla)
                return leftVanilla ? -1 : 1;
            return CompareFacet(left, right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.ToLowerInvariant();
        }

        #endregion
    }
}
