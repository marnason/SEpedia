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

        private struct ScoredEntry
        {
            public CatalogEntry Entry;
            public int Score;
        }

        private static readonly IReadOnlyList<SearchableEntry> EmptyEntries = new List<SearchableEntry>().AsReadOnly();
        private readonly Dictionary<BrowseCategory, List<SearchableEntry>> entriesByCategory;

        public CatalogIndex(DefinitionIndex definitions, IEnumerable<PlanetSnapshot> planets)
        {
            entriesByCategory = new Dictionary<BrowseCategory, List<SearchableEntry>>();
            HashSet<MyDefinitionId> duplicateRecipeNames = FindDuplicateRecipeNames(definitions.All);

            for (int index = 0; index < definitions.All.Count; index++)
            {
                DefinitionDocument definition = definitions.All[index];
                if (definition.BrowseCategory == BrowseCategory.None)
                    continue;

                int celestialOrder = definition.AsteroidGenerator != null ? 1 : 2;
                string listDetail = duplicateRecipeNames.Contains(definition.Id)
                    ? BuildRecipeSummary(definition.Recipe, definitions)
                    : string.Empty;
                Add(new CatalogEntry(definition, celestialOrder, listDetail), definitions);
            }

            if (planets != null)
            {
                foreach (PlanetSnapshot planet in planets)
                    Add(new CatalogEntry(planet), definitions);
            }
        }

        public CatalogResult Query(CatalogFilter filter, int limit)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            NormalizeCategorySelections(filter);
            string query = Normalize(filter.SearchText).Trim();
            string[] tokens = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<SearchableEntry> categoryList;
            IReadOnlyList<SearchableEntry> categoryEntries = entriesByCategory.TryGetValue(filter.Category, out categoryList)
                ? categoryList
                : EmptyEntries;
            List<FacetCount> sources;
            List<FacetCount> blockTypes;
            BuildFacets(categoryEntries, filter, query, tokens, out sources, out blockTypes);

            bool selectionsChanged = RemoveUnavailableSelections(filter.SelectedSourceKeys, sources);
            selectionsChanged |= RemoveUnavailableSelections(filter.SelectedBlockTypes, blockTypes);
            if (selectionsChanged)
                BuildFacets(categoryEntries, filter, query, tokens, out sources, out blockTypes);

            var matches = new List<ScoredEntry>();

            for (int index = 0; index < categoryEntries.Count; index++)
            {
                SearchableEntry searchable = categoryEntries[index];
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
                case BrowseCategory.Recipes: return "Recipes";
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

        private void Add(CatalogEntry entry, DefinitionIndex definitions)
        {
            string subtype = entry.Definition != null ? entry.Definition.SubtypeName : entry.Planet.EntityId.ToString();
            string id = entry.Definition != null ? entry.Definition.Id.ToString() : entry.Planet.EntityId.ToString();
            string runtimeType = entry.Definition != null ? entry.Definition.RuntimeTypeName : "Spawned planet";
            string category = GetCategoryName(entry.Category);
            string name = Normalize(entry.DisplayName);
            string normalizedSubtype = Normalize(subtype);
            string relationships = entry.Definition != null && entry.Definition.Recipe != null
                ? BuildRecipeSearchBlob(entry.Definition.Recipe, definitions)
                : string.Empty;

            var searchable = new SearchableEntry
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

        private static HashSet<MyDefinitionId> FindDuplicateRecipeNames(IReadOnlyList<DefinitionDocument> definitions)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < definitions.Count; index++)
            {
                DefinitionDocument definition = definitions[index];
                if (definition.BrowseCategory != BrowseCategory.Recipes)
                    continue;
                int count;
                counts.TryGetValue(definition.DisplayName, out count);
                counts[definition.DisplayName] = count + 1;
            }

            var duplicates = new HashSet<MyDefinitionId>();
            for (int index = 0; index < definitions.Count; index++)
            {
                DefinitionDocument definition = definitions[index];
                int count;
                if (definition.BrowseCategory == BrowseCategory.Recipes &&
                    counts.TryGetValue(definition.DisplayName, out count) && count > 1)
                    duplicates.Add(definition.Id);
            }
            return duplicates;
        }

        private static string BuildRecipeSummary(RecipeDocument recipe, DefinitionIndex definitions)
        {
            if (recipe == null)
                return "Recipe";
            return JoinItemNames(recipe.Prerequisites, definitions, 2) + " → " +
                JoinItemNames(recipe.Results, definitions, 2);
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

        private static string BuildRecipeSearchBlob(RecipeDocument recipe, DefinitionIndex definitions)
        {
            var builder = new StringBuilder();
            AppendDefinitions(builder, recipe.Prerequisites, definitions);
            AppendDefinitions(builder, recipe.Results, definitions);
            for (int index = 0; index < recipe.ProductionBlocks.Count; index++)
                AppendDefinition(builder, recipe.ProductionBlocks[index], definitions);
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

        private static void AppendDefinition(StringBuilder builder, MyDefinitionId id, DefinitionIndex definitions)
        {
            DefinitionDocument definition;
            if (definitions.TryGet(id, out definition))
                builder.Append(' ').Append(definition.DisplayName).Append(' ').Append(definition.SubtypeName);
            builder.Append(' ').Append(id);
        }

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
                if (!MatchesTriState(entry.Enabled, filter.Enabled) ||
                    !MatchesTriState(entry.Public, filter.Public) ||
                    !MatchesTriState(entry.AvailableInSurvival, filter.AvailableInSurvival) ||
                    Score(searchable, query, tokens) < 0)
                    continue;

                if (MatchesFilters(entry, filter, true, false))
                {
                    string sourceKey = entry.Origin.SourceKey;
                    AddFacet(sourceCounts, sourceNames, sourceKey, entry.Origin.DisplayName);
                }

                if (filter.Category == BrowseCategory.Blocks &&
                    MatchesFilters(entry, filter, false, true))
                {
                    DefinitionDocument definition = entry.Definition;
                    if (definition != null && definition.CubeBlock != null)
                    {
                        string blockTypeKey = definition.RuntimeTypeName;
                        AddFacet(blockTypeCounts, blockTypeNames, blockTypeKey, GetFriendlyRuntimeType(blockTypeKey));
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

        private static bool RemoveUnavailableSelections(HashSet<string> selected, IList<FacetCount> available)
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
