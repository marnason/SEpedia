using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;

namespace SEpedia.Core
{
    internal static class DefinitionIndexBuilder
    {
        public static DefinitionIndex Build(
            MyDefinitionManager manager,
            bool survivalMode,
            Action<string> logWarning)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            var diagnostics = new DefinitionBuildDiagnostics(logWarning);
            List<MyDefinitionBase> sourceDefinitions = CollectDefinitions(manager, diagnostics);
            DefinitionRelationships relationships = DefinitionRelationships.Build(
                manager,
                sourceDefinitions,
                survivalMode,
                diagnostics);
            var iconResolver = new DefinitionIconResolver(manager, diagnostics);
            var extractors = new DefinitionExtractors(relationships, iconResolver, diagnostics);
            var documents = new List<DefinitionDocument>();
            var ids = new HashSet<MyDefinitionId>();

            for (int index = 0; index < sourceDefinitions.Count; index++)
            {
                MyDefinitionBase definition = sourceDefinitions[index];
                if (definition == null)
                {
                    diagnostics.Report("definition-null", "Skipped a null runtime definition.");
                    continue;
                }

                try
                {
                    if (!ids.Add(definition.Id))
                    {
                        diagnostics.Report(
                            "definition-duplicate",
                            "Skipped duplicate definition ID " + definition.Id + ".");
                        continue;
                    }

                    documents.Add(extractors.Extract(definition));
                }
                catch (Exception exception)
                {
                    diagnostics.Report(
                        "definition-extract",
                        "Skipped malformed definition " + definition.Id,
                        exception);
                }
            }

            diagnostics.FlushSuppressedSummaries();
            return new DefinitionIndex(
                documents,
                sourceDefinitions.Count,
                diagnostics.IssueCount,
                iconResolver.GetStats());
        }

        private static List<MyDefinitionBase> CollectDefinitions(
            MyDefinitionManager manager,
            DefinitionBuildDiagnostics diagnostics)
        {
            var definitions = new List<MyDefinitionBase>();
            var ids = new HashSet<MyDefinitionId>();

            try
            {
                foreach (MyDefinitionBase definition in manager.GetAllDefinitions())
                {
                    definitions.Add(definition);
                    if (definition != null)
                        ids.Add(definition.Id);
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "definition-registry",
                    "Could not enumerate the primary definition registry",
                    exception);
            }

            try
            {
                // Blueprints live in a separate registry and are absent from
                // GetAllDefinitions() in the game runtime.
                foreach (MyBlueprintDefinitionBase blueprint in manager.GetBlueprintDefinitions())
                {
                    if (blueprint != null && ids.Add(blueprint.Id))
                        definitions.Add(blueprint);
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "blueprint-registry",
                    "Could not enumerate the blueprint registry",
                    exception);
            }

            return definitions;
        }
    }
}
