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
        private readonly CatalogSchema schema;
        private readonly CatalogEntryVisibility visibility;
        private readonly Dictionary<string, List<SearchableEntry>> entriesByCategory;

        #endregion

        #region Index Construction

        public CatalogIndex(
            CatalogSchema schema,
            CatalogEntryVisibility visibility,
            DefinitionIndex definitions,
            IEnumerable<PlanetSnapshot> planets)
        {
            this.schema = schema;
            this.visibility = visibility;
            entriesByCategory = new Dictionary<string, List<SearchableEntry>>(StringComparer.Ordinal);

            for (int index = 0; index < definitions.All.Count; index++)
            {
                DefinitionDocument definition = definitions.All[index];
                if (string.IsNullOrEmpty(definition.CategoryKey)) continue;
                Add(new CatalogEntry(definition), definitions);
            }

            if (planets != null)
            {
                foreach (PlanetSnapshot planet in planets) Add(new CatalogEntry(planet), definitions);
            }
        }

        private void Add(CatalogEntry entry, DefinitionIndex definitions)
        {
            string subtype = entry.Definition != null ? entry.Definition.SubtypeName : entry.Planet.EntityId.ToString();
            string id = entry.Definition != null ? entry.Definition.Id.ToString() : entry.Planet.EntityId.ToString();
            string runtimeType = entry.Definition != null ? entry.Definition.RuntimeTypeName : "Spawned planet";
            CatalogCategoryDefinition category = schema.GetCategory(entry.CategoryKey);
            string categoryName = category != null ? category.DisplayName : entry.CategoryKey;
            string name = Normalize(entry.DisplayName);
            string normalizedSubtype = Normalize(subtype);
            string relationships = entry.Definition != null && entry.Definition.Recipe != null
                ? CatalogText.BuildRecipeSearchBlob(entry.Definition.Recipe, definitions)
                : string.Empty;
            var facetSearch = new List<string>();
            foreach (FacetValue facet in entry.GetFacetValues())
            {
                facetSearch.Add(Normalize(facet.Key));
                facetSearch.Add(Normalize(facet.DisplayName));
            }

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
                    Normalize(categoryName),
                    Normalize(runtimeType),
                    Normalize(entry.Origin.DisplayName),
                    Normalize(entry.Origin.ModId),
                    Normalize(entry.Origin.ServiceName),
                    Normalize(relationships),
                    string.Join(" ", facetSearch.ToArray())
                })
            };

            List<SearchableEntry> categoryEntries;
            if (!entriesByCategory.TryGetValue(entry.CategoryKey, out categoryEntries))
            {
                categoryEntries = new List<SearchableEntry>();
                entriesByCategory.Add(entry.CategoryKey, categoryEntries);
            }
            categoryEntries.Add(searchable);
        }

        #endregion

        #region Catalog Queries

        public CatalogResult Query(CatalogFilter filter, int offset, int limit)
        {
            return Query(filter, offset, limit, null);
        }

        public bool HasMultipleDefaultEntries(string categoryKey, bool survivalMode)
        {
            List<SearchableEntry> categoryEntries;
            if (!entriesByCategory.TryGetValue(categoryKey, out categoryEntries)) return false;
            var defaultFilter = new CatalogFilter(schema, survivalMode) { CategoryKey = categoryKey };
            int count = 0;
            for (int index = 0; index < categoryEntries.Count; index++)
            {
                if (visibility.IsListVisible(categoryEntries[index].Entry, defaultFilter) && ++count > 1) return true;
            }
            return false;
        }

        public CatalogResult Query(CatalogFilter filter, int offset, int limit, DefinitionDocument includedDefinition)
        {
            if (filter == null) throw new ArgumentNullException("filter");
            List<FacetCount> sources;
            Dictionary<string, IList<FacetCount>> facets;
            List<ScoredEntry> matches = FindMatches(filter, includedDefinition, out sources, out facets);
            int first = Math.Min(Math.Max(0, offset), matches.Count);
            int count = Math.Min(Math.Max(0, limit), matches.Count - first);
            var result = new List<CatalogEntry>(count);
            for (int index = 0; index < count; index++) result.Add(matches[first + index].Entry);
            return new CatalogResult(result, matches.Count, sources, facets);
        }

        public int FindDefinitionResultIndex(CatalogFilter filter, MyDefinitionId definitionId,
            DefinitionDocument includedDefinition, out int totalCount)
        {
            List<FacetCount> sources;
            Dictionary<string, IList<FacetCount>> facets;
            List<ScoredEntry> matches = FindMatches(filter, includedDefinition, out sources, out facets);
            totalCount = matches.Count;
            for (int index = 0; index < matches.Count; index++)
            {
                DefinitionDocument definition = matches[index].Entry.Definition;
                if (definition != null && definition.Id == definitionId) return index;
            }
            return -1;
        }

        private List<ScoredEntry> FindMatches(CatalogFilter filter, DefinitionDocument includedDefinition,
            out List<FacetCount> sources, out Dictionary<string, IList<FacetCount>> facets)
        {
            string query = Normalize(filter.SearchText).Trim();
            string[] tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<SearchableEntry> categoryList;
            IReadOnlyList<SearchableEntry> categoryEntries = entriesByCategory.TryGetValue(filter.CategoryKey, out categoryList)
                ? categoryList : EmptyEntries;
            BuildFacets(categoryEntries, filter, query, tokens, out sources, out facets);

            var matches = new List<ScoredEntry>();
            bool forcedDefinitionIncluded = false;
            for (int index = 0; index < categoryEntries.Count; index++)
            {
                SearchableEntry searchable = categoryEntries[index];
                bool included = includedDefinition != null && searchable.Entry.Definition != null &&
                    searchable.Entry.Definition.Id == includedDefinition.Id;
                bool matchesFilters = visibility.IsListVisible(searchable.Entry, filter);
                int score = Score(searchable, query, tokens);
                bool forceInclude = included && (!matchesFilters || score < 0);
                if (!forceInclude && (!matchesFilters || score < 0)) continue;
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

        private void BuildFacets(IReadOnlyList<SearchableEntry> entries, CatalogFilter filter,
            string query, string[] tokens, out List<FacetCount> sources,
            out Dictionary<string, IList<FacetCount>> facets)
        {
            var sourceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var sourceNames = new Dictionary<string, string>(StringComparer.Ordinal);
            var facetCounts = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            var facetNames = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            CatalogCategoryDefinition category = schema.GetCategory(filter.CategoryKey);
            if (category != null)
            {
                for (int index = 0; index < category.Facets.Count; index++)
                {
                    facetCounts.Add(category.Facets[index].Key, new Dictionary<string, int>(StringComparer.Ordinal));
                    facetNames.Add(category.Facets[index].Key, new Dictionary<string, string>(StringComparer.Ordinal));
                }
            }

            for (int index = 0; index < entries.Count; index++)
            {
                SearchableEntry searchable = entries[index];
                CatalogEntry entry = searchable.Entry;
                if (!visibility.MatchesCommonFlags(entry, filter.Visibility) ||
                    !MatchesBlockAvailability(entry, filter) ||
                    Score(searchable, query, tokens) < 0) continue;

                if (MatchesAllFacets(entry, filter, null))
                    AddFacet(sourceCounts, sourceNames, entry.Origin.SourceKey, entry.Origin.DisplayName);

                if (category == null || !visibility.MatchesSource(entry, filter.Visibility)) continue;
                for (int facetIndex = 0; facetIndex < category.Facets.Count; facetIndex++)
                {
                    string facetKey = category.Facets[facetIndex].Key;
                    FacetValue value;
                    if (MatchesAllFacets(entry, filter, facetKey) && entry.TryGetFacet(facetKey, out value))
                        AddFacet(facetCounts[facetKey], facetNames[facetKey], value.Key, value.DisplayName);
                }
            }

            sources = CreateFacets(sourceCounts, sourceNames);
            sources.Sort(CompareSourceFacet);
            facets = new Dictionary<string, IList<FacetCount>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, Dictionary<string, int>> pair in facetCounts)
            {
                List<FacetCount> values = CreateFacets(pair.Value, facetNames[pair.Key]);
                if (pair.Key == CatalogFacetKeys.CelestialKind)
                    values.Sort(CompareCelestialKindFacet);
                else
                    values.Sort(CompareFacet);
                facets[pair.Key] = values;
            }
        }

        private static bool MatchesBlockAvailability(CatalogEntry entry, CatalogFilter filter)
        {
            if (filter.CategoryKey != CatalogCategoryKeys.Blocks) return true;
            CubeBlockData block = entry.Definition != null ? entry.Definition.CubeBlock : null;
            return block != null &&
                CatalogEntryVisibility.MatchesTriState(block.IsBuildMenuReachable, filter.BuildMenuState) &&
                filter.SelectedGridSizes.Contains(block.CubeSize);
        }

        private static bool MatchesAllFacets(CatalogEntry entry, CatalogFilter filter, string excludedFacetKey)
        {
            CatalogCategoryDefinition category = filter.Schema.GetCategory(filter.CategoryKey);
            if (category == null) return true;
            for (int index = 0; index < category.Facets.Count; index++)
            {
                string facetKey = category.Facets[index].Key;
                if (facetKey == excludedFacetKey) continue;
                HashSet<string> selected = filter.GetSelectedFacetValues(facetKey);
                FacetValue value;
                if (selected.Count > 0 && (!entry.TryGetFacet(facetKey, out value) || !selected.Contains(value.Key)))
                    return false;
            }
            return true;
        }

        private static void AddFacet(IDictionary<string, int> counts, IDictionary<string, string> names,
            string key, string displayName)
        {
            int count;
            counts.TryGetValue(key, out count);
            counts[key] = count + 1;
            names[key] = displayName;
        }

        private static List<FacetCount> CreateFacets(IDictionary<string, int> counts,
            IDictionary<string, string> names)
        {
            var result = new List<FacetCount>(counts.Count);
            foreach (KeyValuePair<string, int> pair in counts)
                result.Add(new FacetCount(pair.Key, names[pair.Key], pair.Value));
            return result;
        }

        #endregion

        #region Search Scoring and Ordering

        private static int Score(SearchableEntry entry, string query, string[] tokens)
        {
            if (tokens.Length == 0) return 0;
            for (int index = 0; index < tokens.Length; index++)
            {
                if (entry.Blob.IndexOf(tokens[index], StringComparison.Ordinal) < 0) return -1;
            }
            int score = tokens.Length * 10;
            if (entry.Name == query) score += 1000;
            else if (entry.Subtype == query) score += 950;
            else if (entry.Name.StartsWith(query, StringComparison.Ordinal)) score += 700;
            else if (entry.Subtype.StartsWith(query, StringComparison.Ordinal)) score += 650;
            else if (entry.Name.IndexOf(query, StringComparison.Ordinal) >= 0) score += 400;
            else if (entry.Subtype.IndexOf(query, StringComparison.Ordinal) >= 0) score += 350;
            return score;
        }

        private static int CompareScored(ScoredEntry left, ScoredEntry right)
        {
            if (left.Entry.CategoryKey == CatalogCategoryKeys.Celestial &&
                right.Entry.CategoryKey == CatalogCategoryKeys.Celestial)
            {
                int kind = CompareCelestialKinds(left.Entry, right.Entry);
                if (kind != 0) return kind;
            }
            int score = right.Score.CompareTo(left.Score);
            return score != 0 ? score : CompareAlphabetically(left, right);
        }

        private static int CompareAlphabetically(ScoredEntry left, ScoredEntry right)
        {
            if (left.Entry.CategoryKey == CatalogCategoryKeys.Celestial &&
                right.Entry.CategoryKey == CatalogCategoryKeys.Celestial)
            {
                int kind = CompareCelestialKinds(left.Entry, right.Entry);
                if (kind != 0) return kind;
            }
            int name = string.Compare(left.Entry.DisplayName, right.Entry.DisplayName, StringComparison.OrdinalIgnoreCase);
            return name != 0 ? name : string.Compare(left.Entry.StableKey, right.Entry.StableKey, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareFacet(FacetCount left, FacetCount right)
        {
            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareCelestialKinds(CatalogEntry left, CatalogEntry right)
        {
            if (left.IsSpawnedPlanet != right.IsSpawnedPlanet) return left.IsSpawnedPlanet ? -1 : 1;
            int name = string.Compare(left.CelestialKindDisplayName, right.CelestialKindDisplayName,
                StringComparison.OrdinalIgnoreCase);
            return name != 0 ? name : string.Compare(left.CelestialKindKey, right.CelestialKindKey,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareCelestialKindFacet(FacetCount left, FacetCount right)
        {
            bool leftSpawned = left.Key == "spawned";
            bool rightSpawned = right.Key == "spawned";
            if (leftSpawned != rightSpawned) return leftSpawned ? -1 : 1;
            return CompareFacet(left, right);
        }

        private static int CompareSourceFacet(FacetCount left, FacetCount right)
        {
            bool leftVanilla = left.Key == "vanilla";
            bool rightVanilla = right.Key == "vanilla";
            if (leftVanilla != rightVanilla) return leftVanilla ? -1 : 1;
            return CompareFacet(left, right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.ToLowerInvariant();
        }

        #endregion
    }
}
