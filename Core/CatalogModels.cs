using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace SEpedia.Core
{
    public sealed class PlanetSnapshot
    {
        public long EntityId { get; private set; }
        public string DisplayName { get; private set; }
        public Vector3D Position { get; private set; }
        public float MinimumRadius { get; private set; }
        public float AverageRadius { get; private set; }
        public float MaximumRadius { get; private set; }
        public bool HasAtmosphere { get; private set; }
        public float AtmosphereRadius { get; private set; }
        public float AtmosphereAltitude { get; private set; }
        public MyDefinitionId? GeneratorId { get; private set; }
        public DefinitionOrigin Origin { get; private set; }
        public bool Enabled { get; private set; }
        public bool Public { get; private set; }
        public bool AvailableInSurvival { get; private set; }
        public PlanetGeneratorData GeneratorData { get; private set; }
        public bool HasGeneratorMetadata { get; private set; }

        public PlanetSnapshot(
            long entityId,
            string displayName,
            Vector3D position,
            float minimumRadius,
            float averageRadius,
            float maximumRadius,
            bool hasAtmosphere,
            float atmosphereRadius,
            float atmosphereAltitude,
            MyDefinitionId? generatorId,
            DefinitionOrigin origin,
            bool enabled,
            bool isPublic,
            bool availableInSurvival,
            PlanetGeneratorData generatorData,
            bool hasGeneratorMetadata)
        {
            EntityId = entityId;
            DisplayName = displayName ?? string.Empty;
            Position = position;
            MinimumRadius = minimumRadius;
            AverageRadius = averageRadius;
            MaximumRadius = maximumRadius;
            HasAtmosphere = hasAtmosphere;
            AtmosphereRadius = atmosphereRadius;
            AtmosphereAltitude = atmosphereAltitude;
            GeneratorId = generatorId;
            Origin = origin ?? DefinitionOrigin.Unknown;
            Enabled = enabled;
            Public = isPublic;
            AvailableInSurvival = availableInSurvival;
            GeneratorData = generatorData;
            HasGeneratorMetadata = hasGeneratorMetadata;
        }
    }

    public sealed class CatalogEntry
    {
        public DefinitionDocument Definition { get; private set; }
        public PlanetSnapshot Planet { get; private set; }
        public BrowseCategory Category { get; private set; }
        public string DisplayName { get; private set; }
        public string StableKey { get; private set; }
        public string ListDetail { get; private set; }
        public int CelestialSortOrder { get; private set; }

        public bool IsSpawnedPlanet
        {
            get { return Planet != null; }
        }

        public DefinitionOrigin Origin
        {
            get { return Definition != null ? Definition.Origin : Planet.Origin; }
        }

        public bool Enabled
        {
            get { return Definition != null ? Definition.Enabled : Planet.Enabled; }
        }

        public bool Public
        {
            get { return Definition != null ? Definition.Public : Planet.Public; }
        }

        public bool AvailableInSurvival
        {
            get
            {
                if (Definition != null && Definition.Recipe != null &&
                    Definition.BrowseCategory == BrowseCategory.Recipes)
                    return Definition.Recipe.ProductionMenuReachable;
                return Definition != null ? Definition.AvailableInSurvival : Planet.AvailableInSurvival;
            }
        }

        public CatalogEntry(DefinitionDocument definition, int celestialSortOrder, string listDetail = null)
        {
            Definition = definition;
            Category = definition.BrowseCategory;
            DisplayName = definition.DisplayName;
            StableKey = "definition:" + definition.Id;
            ListDetail = listDetail ?? string.Empty;
            CelestialSortOrder = celestialSortOrder;
        }

        public CatalogEntry(PlanetSnapshot planet)
        {
            Planet = planet;
            Category = BrowseCategory.Celestial;
            DisplayName = planet.DisplayName;
            StableKey = "planet:" + planet.EntityId;
            ListDetail = string.Empty;
            CelestialSortOrder = 0;
        }
    }

    public sealed class CatalogFilter
    {
        public BrowseCategory Category { get; set; }
        public string SearchText { get; set; }
        public TriStateFilter Enabled { get; set; }
        public TriStateFilter Public { get; set; }
        public TriStateFilter AvailableInSurvival { get; set; }
        public TriStateFilter ListedInBuildMenu { get; set; }
        public HashSet<string> SelectedSourceKeys { get; private set; }
        public HashSet<MyCubeSize> SelectedGridSizes { get; private set; }
        public HashSet<string> SelectedBlockTypes { get; private set; }

        public CatalogFilter(bool survivalMode)
        {
            SelectedSourceKeys = new HashSet<string>(StringComparer.Ordinal);
            SelectedGridSizes = new HashSet<MyCubeSize>();
            SelectedBlockTypes = new HashSet<string>(StringComparer.Ordinal);
            Category = BrowseCategory.Components;
            SearchText = string.Empty;
            ResetAdvanced(survivalMode);
        }

        public void ResetAdvanced(bool survivalMode)
        {
            Enabled = TriStateFilter.Yes;
            Public = TriStateFilter.Yes;
            AvailableInSurvival = survivalMode ? TriStateFilter.Yes : TriStateFilter.Either;
            ListedInBuildMenu = TriStateFilter.Yes;
            SelectedSourceKeys.Clear();
            SelectedGridSizes.Clear();
            SelectedGridSizes.Add(MyCubeSize.Small);
            SelectedGridSizes.Add(MyCubeSize.Large);
            SelectedBlockTypes.Clear();
        }
    }

    public sealed class FacetCount
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

    public sealed class CatalogResult
    {
        public IReadOnlyList<CatalogEntry> Items { get; private set; }
        public int TotalCount { get; private set; }
        public IReadOnlyList<FacetCount> Sources { get; private set; }
        public IReadOnlyList<FacetCount> BlockTypes { get; private set; }

        public CatalogResult(
            IList<CatalogEntry> items,
            int totalCount,
            IList<FacetCount> sources,
            IList<FacetCount> blockTypes)
        {
            Items = new List<CatalogEntry>(items).AsReadOnly();
            TotalCount = totalCount;
            Sources = new List<FacetCount>(sources).AsReadOnly();
            BlockTypes = new List<FacetCount>(blockTypes).AsReadOnly();
        }
    }
}
