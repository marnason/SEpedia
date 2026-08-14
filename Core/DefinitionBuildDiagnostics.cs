using System;
using System.Collections.Generic;

namespace SEpedia.Core
{
    internal sealed class DefinitionBuildDiagnostics
    {
        #region State and Construction

        private const int SampleLimit = 5;

        private readonly Action<string> logWarning;
        private readonly Dictionary<string, int> counts;

        public int IssueCount { get; private set; }

        public DefinitionBuildDiagnostics(Action<string> logWarning)
        {
            this.logWarning = logWarning;
            counts = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        #endregion

        #region Reporting

        public void Report(string code, string message)
        {
            IssueCount++;

            int count;
            counts.TryGetValue(code, out count);
            count++;
            counts[code] = count;

            if (count <= SampleLimit && logWarning != null)
                logWarning("[" + code + "] " + message);
        }

        public void Report(string code, string subject, Exception exception)
        {
            Report(code, subject + ": " + exception.Message);
        }

        public void FlushSuppressedSummaries()
        {
            if (logWarning == null)
                return;

            foreach (KeyValuePair<string, int> pair in counts)
            {
                int suppressed = pair.Value - SampleLimit;
                if (suppressed > 0)
                {
                    logWarning(
                        "[" + pair.Key + "] Suppressed " + suppressed +
                        " additional issue" + (suppressed == 1 ? "." : "s."));
                }
            }
        }

        #endregion
    }
}
