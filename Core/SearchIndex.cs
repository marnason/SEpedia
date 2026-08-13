using System;
using System.Collections.Generic;

namespace SEpedia.Core
{
    public sealed class SearchIndex
    {
        private sealed class SearchableDefinition
        {
            public DefinitionDocument Document;
            public string Name;
            public string Subtype;
            public string Blob;
        }

        private sealed class ScoredDefinition
        {
            public DefinitionDocument Document;
            public int Score;
        }

        private readonly List<SearchableDefinition> entries;

        public SearchIndex(IEnumerable<DefinitionDocument> definitions)
        {
            entries = new List<SearchableDefinition>();

            foreach (DefinitionDocument definition in definitions)
            {
                string name = Normalize(definition.DisplayName);
                string subtype = Normalize(definition.SubtypeName);
                string blob = string.Join(" ", new[]
                {
                    name,
                    subtype,
                    Normalize(definition.Id.ToString()),
                    Normalize(definition.Categories.ToString()),
                    Normalize(definition.RuntimeTypeName),
                    Normalize(definition.Origin.DisplayName),
                    Normalize(definition.Origin.ModId),
                    Normalize(definition.Origin.ServiceName),
                    Normalize(definition.Origin.SourceFile)
                });

                entries.Add(new SearchableDefinition
                {
                    Document = definition,
                    Name = name,
                    Subtype = subtype,
                    Blob = blob
                });
            }
        }

        public SearchResult Search(string query, int limit)
        {
            string normalizedQuery = Normalize(query).Trim();
            string[] tokens = normalizedQuery.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var matches = new List<ScoredDefinition>();

            for (int index = 0; index < entries.Count; index++)
            {
                SearchableDefinition entry = entries[index];
                int score = Score(entry, normalizedQuery, tokens);

                if (score >= 0)
                    matches.Add(new ScoredDefinition { Document = entry.Document, Score = score });
            }

            matches.Sort(Compare);

            int resultCount = Math.Min(Math.Max(limit, 0), matches.Count);
            var results = new List<DefinitionDocument>(resultCount);

            for (int index = 0; index < resultCount; index++)
                results.Add(matches[index].Document);

            return new SearchResult(results, matches.Count);
        }

        private static int Score(SearchableDefinition entry, string query, string[] tokens)
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

        private static int Compare(ScoredDefinition left, ScoredDefinition right)
        {
            int scoreComparison = right.Score.CompareTo(left.Score);
            if (scoreComparison != 0)
                return scoreComparison;

            int nameComparison = string.Compare(left.Document.DisplayName, right.Document.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (nameComparison != 0)
                return nameComparison;

            return string.Compare(left.Document.Id.ToString(), right.Document.Id.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.ToLowerInvariant();
        }
    }
}
