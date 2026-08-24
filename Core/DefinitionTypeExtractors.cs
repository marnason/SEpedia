using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;

namespace SEpedia.Core
{
    internal sealed class PlanetOreResolver
    {
        private sealed class ResolvedVoxelMaterial
        {
            public MyDefinitionId OreId;
            public PlanetVoxelMaterialData VoxelMaterial;
        }

        private readonly Dictionary<string, ResolvedVoxelMaterial> oreByVoxelMaterial;

        public PlanetOreResolver(
            MyDefinitionManager manager,
            DefinitionBuildDiagnostics diagnostics)
        {
            oreByVoxelMaterial = new Dictionary<string, ResolvedVoxelMaterial>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (MyVoxelMaterialDefinition material in manager.GetVoxelMaterialDefinitions())
                {
                    if (material == null || string.IsNullOrWhiteSpace(material.Id.SubtypeName) ||
                        string.IsNullOrWhiteSpace(material.MinedOre))
                        continue;

                    oreByVoxelMaterial[material.Id.SubtypeName] = new ResolvedVoxelMaterial
                    {
                        OreId = new MyDefinitionId(typeof(MyObjectBuilder_Ore), material.MinedOre),
                        VoxelMaterial = new PlanetVoxelMaterialData(
                            material.Id.SubtypeName,
                            material.DisplayNameText,
                            material.MinedOre,
                            material.MinedOreRatio,
                            material.CanBeHarvested,
                            material.IsRare,
                            material.SpawnsInAsteroids,
                            material.SpawnsFromMeteorites,
                            material.AsteroidGeneratorSpawnProbabilityMultiplier)
                    };
                }
            }
            catch (Exception exception)
            {
                diagnostics.Report(
                    "voxel-material-registry",
                    "Could not enumerate voxel materials for planet ore mappings",
                    exception);
            }
        }

        public bool TryResolve(
            string voxelMaterial,
            out MyDefinitionId oreId,
            out PlanetVoxelMaterialData materialData)
        {
            oreId = default(MyDefinitionId);
            materialData = null;
            ResolvedVoxelMaterial resolved;
            if (string.IsNullOrWhiteSpace(voxelMaterial) ||
                !oreByVoxelMaterial.TryGetValue(voxelMaterial, out resolved))
                return false;
            oreId = resolved.OreId;
            materialData = resolved.VoxelMaterial;
            return true;
        }
    }

    internal static class PhysicalDefinitionExtractor
    {
        public static PhysicalItemData Extract(
            MyPhysicalItemDefinition definition,
            DefinitionBuildDiagnostics diagnostics)
        {
            try
            {
                return new PhysicalItemData(definition.Mass, definition.Volume);
            }
            catch (Exception exception)
            {
                diagnostics.Report("physical-item", "Could not read " + definition.Id, exception);
                return null;
            }
        }
    }

    internal static class ProductionDefinitionExtractor
    {
        public static RecipeDocument Extract(
            MyBlueprintDefinitionBase definition,
            DefinitionRelationships relationships,
            DefinitionBuildDiagnostics diagnostics)
        {
            try
            {
                return new RecipeDocument(
                    definition.Id,
                    definition.BaseProductionTimeInSeconds,
                    definition.Atomic,
                    ExtractItems(definition, "prerequisite", definition.Prerequisites, diagnostics),
                    ExtractItems(definition, "result", definition.Results, diagnostics),
                    new List<MyDefinitionId>(relationships.GetProductionBlocks(definition.Id)));
            }
            catch (Exception exception)
            {
                diagnostics.Report("recipe", "Could not read " + definition.Id, exception);
                return null;
            }
        }

        private static List<DefinitionAmount> ExtractItems(
            MyBlueprintDefinitionBase definition,
            string relationship,
            MyBlueprintDefinitionBase.Item[] source,
            DefinitionBuildDiagnostics diagnostics)
        {
            var result = new List<DefinitionAmount>();
            if (source == null)
                return result;
            for (int index = 0; index < source.Length; index++)
            {
                try
                {
                    result.Add(new DefinitionAmount(source[index].Id, source[index].Amount));
                }
                catch (Exception exception)
                {
                    diagnostics.Report(
                        "recipe-" + relationship,
                        "Skipped entry in " + definition.Id,
                        exception);
                }
            }
            return result;
        }
    }

    internal static class CubeBlockDefinitionExtractor
    {
        public static CubeBlockData Extract(
            MyCubeBlockDefinition definition,
            DefinitionRelationships relationships,
            DefinitionBuildDiagnostics diagnostics)
        {
            try
            {
                var requirements = new List<BlockComponentRequirement>();
                MyCubeBlockDefinition.Component[] components = definition.Components;
                if (components != null)
                {
                    for (int index = 0; index < components.Length; index++)
                    {
                        try
                        {
                            MyCubeBlockDefinition.Component component = components[index];
                            if (component == null || component.Definition == null)
                                throw new InvalidOperationException("Component definition is missing.");
                            requirements.Add(new BlockComponentRequirement(component.Definition.Id, component.Count));
                        }
                        catch (Exception exception)
                        {
                            diagnostics.Report("block-component", "Skipped entry in " + definition.Id, exception);
                        }
                    }
                }

                return new CubeBlockData(
                    definition.CubeSize,
                    definition.Size,
                    definition.PCU,
                    relationships.IsBuildMenuReachable(definition.Id),
                    definition.BlockPairName,
                    new List<MyDefinitionId>(relationships.GetRelatedBlocks(definition.Id)),
                    requirements);
            }
            catch (Exception exception)
            {
                diagnostics.Report("cube-block", "Could not read " + definition.Id, exception);
                return null;
            }
        }
    }

    internal static class CelestialDefinitionExtractor
    {
        public static PlanetGeneratorData ExtractPlanet(
            MyPlanetGeneratorDefinition definition,
            PlanetOreResolver oreResolver,
            DefinitionBuildDiagnostics diagnostics)
        {
            try
            {
                var weatherTypes = new List<string>();
                if (definition.WeatherGenerators != null)
                {
                    for (int generatorIndex = 0; generatorIndex < definition.WeatherGenerators.Count; generatorIndex++)
                    {
                        try
                        {
                            MyWeatherGeneratorSettings generator = definition.WeatherGenerators[generatorIndex];
                            if (generator == null || generator.Weathers == null)
                                continue;
                            for (int weatherIndex = 0; weatherIndex < generator.Weathers.Count; weatherIndex++)
                            {
                                MyWeatherGeneratorVoxelSettings weather = generator.Weathers[weatherIndex];
                                if (weather != null && !string.IsNullOrWhiteSpace(weather.Name))
                                    weatherTypes.Add(weather.Name + " (weight " + weather.Weight + ")");
                            }
                        }
                        catch (Exception exception)
                        {
                            diagnostics.Report("planet-weather", "Skipped entry in " + definition.Id, exception);
                        }
                    }
                }

                var ores = new List<PlanetOreData>();
                if (definition.OreMappings != null)
                {
                    for (int index = 0; index < definition.OreMappings.Length; index++)
                    {
                        try
                        {
                            MyPlanetOreMapping ore = definition.OreMappings[index];
                            if (ore != null)
                            {
                                MyDefinitionId oreId;
                                PlanetVoxelMaterialData voxelMaterial;
                                bool resolved = oreResolver.TryResolve(ore.Type, out oreId, out voxelMaterial);
                                if (!resolved)
                                {
                                    diagnostics.Report(
                                        "planet-ore-material",
                                        "Could not resolve voxel material " + ore.Type + " in " + definition.Id + ".");
                                }
                                ores.Add(new PlanetOreData(
                                    ore.Type,
                                    resolved ? (MyDefinitionId?)oreId : null,
                                    ore.Value,
                                    ore.Start,
                                    ore.Depth,
                                    ore.TargetColor,
                                    ore.ColorInfluence,
                                    ore.ColorShift.HasValue ? ore.ColorShift.Value.ToString() : string.Empty,
                                    voxelMaterial));
                            }
                        }
                        catch (Exception exception)
                        {
                            diagnostics.Report("planet-ore", "Skipped entry in " + definition.Id, exception);
                        }
                    }
                }

                MyPlanetAtmosphere atmosphere = definition.Atmosphere;
                return new PlanetGeneratorData(
                    definition.SurfaceGravity,
                    definition.GravityFalloffPower,
                    definition.HasAtmosphere,
                    definition.AtmosphereHeight,
                    atmosphere != null && atmosphere.Breathable,
                    atmosphere != null ? atmosphere.Density : 0f,
                    atmosphere != null ? atmosphere.OxygenDensity : 0f,
                    atmosphere != null ? atmosphere.LimitAltitude : 0f,
                    atmosphere != null ? atmosphere.MaxWindSpeed : 0f,
                    definition.DefaultSurfaceTemperature.ToString(),
                    definition.WeatherFrequencyMin,
                    definition.WeatherFrequencyMax,
                    definition.PersistentWeather,
                    weatherTypes,
                    ores);
            }
            catch (Exception exception)
            {
                diagnostics.Report("planet-generator", "Could not read " + definition.Id, exception);
                return null;
            }
        }

        public static AsteroidGeneratorData ExtractAsteroid(
            MyAsteroidGeneratorDefinition definition,
            DefinitionBuildDiagnostics diagnostics)
        {
            try
            {
                var seedProbabilities = new List<string>();
                foreach (KeyValuePair<MyObjectSeedType, double> pair in definition.SeedTypeProbability)
                    seedProbabilities.Add(pair.Key + ": " + pair.Value.ToString("0.###"));
                var clusterProbabilities = new List<string>();
                foreach (KeyValuePair<MyObjectSeedType, double> pair in definition.SeedClusterTypeProbability)
                    clusterProbabilities.Add(pair.Key + ": " + pair.Value.ToString("0.###"));

                return new AsteroidGeneratorData(
                    definition.Version,
                    definition.ObjectSizeMin,
                    definition.ObjectSizeMax,
                    definition.ObjectSizeMinCluster,
                    definition.ObjectSizeMaxCluster,
                    definition.ObjectMaxInCluster,
                    definition.ObjectMinDistanceInCluster,
                    definition.ObjectMaxDistanceInClusterMin,
                    definition.ObjectMaxDistanceInClusterMax,
                    definition.ObjectDensityCluster,
                    definition.ClusterDispersionAbsolute,
                    definition.RotateAsteroids,
                    definition.UseClusterVariableSize,
                    seedProbabilities,
                    clusterProbabilities);
            }
            catch (Exception exception)
            {
                diagnostics.Report("asteroid-generator", "Could not read " + definition.Id, exception);
                return null;
            }
        }
    }
}
