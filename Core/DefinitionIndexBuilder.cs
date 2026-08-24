using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;

namespace SEpedia.Core
{
    internal static class DefinitionIndexBuilder
    {
        #region Index Construction

        public static DefinitionIndex Build(
            MyDefinitionManager manager,
            bool survivalMode,
            Action<string> logWarning)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");

            var diagnostics = new DefinitionBuildDiagnostics(logWarning);
            int planetGeneratorCount;
            int asteroidGeneratorCount;
            List<MyDefinitionBase> sourceDefinitions = CollectDefinitions(
                manager,
                diagnostics,
                out planetGeneratorCount,
                out asteroidGeneratorCount);
            DefinitionRelationships relationships = DefinitionRelationships.Build(
                manager,
                sourceDefinitions,
                survivalMode,
                diagnostics);
            var extractors = new DefinitionExtractors(manager, relationships, diagnostics);
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
                planetGeneratorCount,
                asteroidGeneratorCount);
        }

        #endregion

        #region Registry Enumeration

        private static List<MyDefinitionBase> CollectDefinitions(
            MyDefinitionManager manager,
            DefinitionBuildDiagnostics diagnostics,
            out int planetGeneratorCount,
            out int asteroidGeneratorCount)
        {
            var definitions = new List<MyDefinitionBase>();
            var ids = new HashSet<MyDefinitionId>();
            var blockBlueprintIds = new HashSet<MyDefinitionId>();
            planetGeneratorCount = 0;
            asteroidGeneratorCount = 0;

            try
            {
                foreach (MyDefinitionBase definition in manager.GetAllDefinitions())
                {
                    if (definition != null)
                    {
                        if (definition is MyCubeBlockDefinition)
                        {
                            blockBlueprintIds.Add(new MyDefinitionId(
                                typeof(MyObjectBuilder_BlueprintDefinition),
                                definition.Id.ToString().Replace("MyObjectBuilder_", string.Empty)));
                        }
                    }
                    AddDefinition(definitions, ids, definition);
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
                    if (blueprint != null && !blockBlueprintIds.Contains(blueprint.Id))
                        AddDefinition(definitions, ids, blueprint);
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "blueprint-registry",
                    "Could not enumerate the blueprint registry",
                    exception);
            }

            try
            {
                foreach (MyPlanetGeneratorDefinition planet in manager.GetPlanetsGeneratorsDefinitions())
                {
                    if (planet != null)
                        planetGeneratorCount++;
                    AddDefinition(definitions, ids, planet);
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "planet-generator-registry",
                    "Could not enumerate the planet generator registry",
                    exception);
            }

            try
            {
                foreach (MyAsteroidGeneratorDefinition asteroid in manager.GetAsteroidGeneratorDefinitions().Values)
                {
                    if (asteroid != null)
                        asteroidGeneratorCount++;
                    AddDefinition(definitions, ids, asteroid);
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "asteroid-generator-registry",
                    "Could not enumerate the asteroid generator registry",
                    exception);
            }

            return definitions;
        }

        private static void AddDefinition(
            ICollection<MyDefinitionBase> definitions,
            ISet<MyDefinitionId> ids,
            MyDefinitionBase definition)
        {
            if (definition == null)
            {
                definitions.Add(null);
                return;
            }

            if (ids.Add(definition.Id))
                definitions.Add(definition);
        }

        #endregion
    }
}
