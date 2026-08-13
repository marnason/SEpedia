using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;

namespace SEpedia.Core
{
    public sealed class CatalogIndex
    {
        private sealed class SearchableEntry
        {
            public CatalogEntry Entry;
            public string Name;
            public string Subtype;
            public string Blob;
        }

        private sealed class ScoredEntry
        {
            public CatalogEntry Entry;
            public int Score;
        }

        private readonly List<SearchableEntry> entries;

        public CatalogIndex(DefinitionIndex definitions, IEnumerable<PlanetSnapshot> planets)
        {
            entries = new List<SearchableEntry>();

            for (int index = 0; index < definitions.All.Count; index++)
            {
                DefinitionDocument definition = definitions.All[index];
                if (definition.BrowseCategory == BrowseCategory.None)
                    continue;

                int celestialOrder = definition.AsteroidGenerator != null ? 1 : 2;
                Add(new CatalogEntry(definition, celestialOrder));
            }

            if (planets != null)
            {
                foreach (PlanetSnapshot planet in planets)
                    Add(new CatalogEntry(planet));
            }
        }

        public CatalogResult Query(CatalogFilter filter, int limit)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            NormalizeCategorySelections(filter);

            List<FacetCount> blockTypes = BuildFacets(filter, true);
            RemoveUnavailableSelections(filter.SelectedBlockTypes, blockTypes);

            List<FacetCount> sources = BuildFacets(filter, false);
            RemoveUnavailableSelections(filter.SelectedSourceKeys, sources);

            blockTypes = BuildFacets(filter, true);
            RemoveUnavailableSelections(filter.SelectedBlockTypes, blockTypes);
            sources = BuildFacets(filter, false);

            string query = Normalize(filter.SearchText).Trim();
            string[] tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var matches = new List<ScoredEntry>();

            for (int index = 0; index < entries.Count; index++)
            {
                SearchableEntry searchable = entries[index];
                if (!MatchesFilters(searchable.Entry, filter, false, false))
                    continue;

                int score = Score(searchable, query, tokens);
                if (score >= 0)
                    matches.Add(new ScoredEntry { Entry = searchable.Entry, Score = score });
            }

            matches.Sort(CompareScored);
            int count = Math.Min(Math.Max(0, limit), matches.Count);
            var result = new List<CatalogEntry>(count);
            for (int index = 0; index < count; index++)
                result.Add(matches[index].Entry);

            return new CatalogResult(result, matches.Count, sources, blockTypes);
        }

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
                case BrowseCategory.Celestial: return "Celestial";
                default: return "Entries";
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

        private void Add(CatalogEntry entry)
        {
            string subtype = entry.Definition != null ? entry.Definition.SubtypeName : entry.Planet.EntityId.ToString();
            string id = entry.Definition != null ? entry.Definition.Id.ToString() : entry.Planet.EntityId.ToString();
            string runtimeType = entry.Definition != null ? entry.Definition.RuntimeTypeName : "Spawned planet";
            string category = GetCategoryName(entry.Category);
            string name = Normalize(entry.DisplayName);
            string normalizedSubtype = Normalize(subtype);

            entries.Add(new SearchableEntry
            {
                Entry = entry,
                Name = name,
                Subtype = normalizedSubtype,
                Blob = string.Join(" ", new[]
                {
                    name,
                    normalizedSubtype,
                    Normalize(id),
                    Normalize(category),
                    Normalize(runtimeType),
                    Normalize(entry.Origin.DisplayName),
                    Normalize(entry.Origin.ModId),
                    Normalize(entry.Origin.ServiceName)
                })
            });
        }

        private List<FacetCount> BuildFacets(CatalogFilter filter, bool blockTypeFacet)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var displayNames = new Dictionary<string, string>(StringComparer.Ordinal);
            string query = Normalize(filter.SearchText).Trim();
            string[] tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int index = 0; index < entries.Count; index++)
            {
                SearchableEntry searchable = entries[index];
                if (!MatchesFilters(searchable.Entry, filter, !blockTypeFacet, blockTypeFacet) ||
                    Score(searchable, query, tokens) < 0)
                    continue;

                string key;
                string display;
                if (blockTypeFacet)
                {
                    if (searchable.Entry.Definition == null || searchable.Entry.Definition.CubeBlock == null)
                        continue;
                    key = searchable.Entry.Definition.RuntimeTypeName;
                    display = GetFriendlyRuntimeType(key);
                }
                else
                {
                    key = searchable.Entry.Origin.SourceKey;
                    display = searchable.Entry.Origin.DisplayName;
                }

                int count;
                counts.TryGetValue(key, out count);
                counts[key] = count + 1;
                displayNames[key] = display;
            }

            var result = new List<FacetCount>();
            foreach (KeyValuePair<string, int> pair in counts)
                result.Add(new FacetCount(pair.Key, displayNames[pair.Key], pair.Value));

            if (blockTypeFacet)
                result.Sort(CompareFacet);
            else
                result.Sort(CompareSourceFacet);
            return result;
        }

        private static bool MatchesFilters(CatalogEntry entry, CatalogFilter filter, bool ignoreSource, bool ignoreBlockType)
        {
            if (entry.Category != filter.Category ||
                !MatchesTriState(entry.Enabled, filter.Enabled) ||
                !MatchesTriState(entry.Public, filter.Public) ||
                !MatchesTriState(entry.AvailableInSurvival, filter.AvailableInSurvival))
                return false;

            if (!ignoreSource && filter.SelectedSourceKeys.Count > 0 &&
                !filter.SelectedSourceKeys.Contains(entry.Origin.SourceKey))
                return false;

            if (filter.Category == BrowseCategory.Blocks)
            {
                DefinitionDocument definition = entry.Definition;
                CubeBlockData block = definition != null ? definition.CubeBlock : null;
                if (block == null ||
                    !MatchesTriState(block.BuildMenuReachable, filter.ListedInBuildMenu) ||
                    !filter.SelectedGridSizes.Contains(block.CubeSize))
                    return false;

                if (!ignoreBlockType && filter.SelectedBlockTypes.Count > 0 &&
                    !filter.SelectedBlockTypes.Contains(definition.RuntimeTypeName))
                    return false;
            }

            return true;
        }

        private static bool MatchesTriState(bool value, TriStateFilter filter)
        {
            return filter == TriStateFilter.Either ||
                (filter == TriStateFilter.Yes && value) ||
                (filter == TriStateFilter.No && !value);
        }

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

        private static void NormalizeCategorySelections(CatalogFilter filter)
        {
            if (filter.Category != BrowseCategory.Blocks)
            {
                filter.SelectedBlockTypes.Clear();
                filter.SelectedGridSizes.Clear();
                return;
            }

            if (filter.SelectedGridSizes.Count == 0)
            {
                filter.SelectedGridSizes.Add(MyCubeSize.Small);
                filter.SelectedGridSizes.Add(MyCubeSize.Large);
            }
        }

        private static void RemoveUnavailableSelections(HashSet<string> selected, IList<FacetCount> available)
        {
            if (selected.Count == 0)
                return;

            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < available.Count; index++)
                keys.Add(available[index].Key);
            selected.RemoveWhere(delegate(string key) { return !keys.Contains(key); });
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
    }
}
