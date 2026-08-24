using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.ModAPI;

namespace SEpedia.Core
{
    internal sealed class CelestialIndex
    {
        #region State

        public event Action Changed;

        private readonly DefinitionIndex definitions;
        private readonly Action<string> logWarning;
        private readonly Dictionary<long, PlanetSnapshot> planets;
        private readonly HashSet<MyDefinitionId> unresolvedGeneratorIds;
        private bool subscribed;

        public IReadOnlyList<PlanetSnapshot> Planets
        {
            get
            {
                var result = new List<PlanetSnapshot>(planets.Values);
                result.Sort(ComparePlanets);
                return result.AsReadOnly();
            }
        }

        #endregion

        #region Construction and Lifecycle

        public CelestialIndex(DefinitionIndex definitions, Action<string> logWarning)
        {
            this.definitions = definitions;
            this.logWarning = logWarning;
            planets = new Dictionary<long, PlanetSnapshot>();
            unresolvedGeneratorIds = new HashSet<MyDefinitionId>();
        }

        public void Initialize()
        {
            if (subscribed || MyAPIGateway.Entities == null)
                return;

            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            foreach (IMyEntity entity in entities)
                TryAdd(entity, false);

            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
            subscribed = true;
        }

        public void Close()
        {
            if (subscribed && MyAPIGateway.Entities != null)
            {
                MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
                MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            }

            subscribed = false;
            planets.Clear();
            unresolvedGeneratorIds.Clear();
            Changed = null;
        }

        #endregion

        #region Entity Tracking

        private void OnEntityAdd(IMyEntity entity)
        {
            TryAdd(entity, true);
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            if (entity == null || !planets.Remove(entity.EntityId))
                return;

            RaiseChanged();
        }

        private void TryAdd(IMyEntity entity, bool notify)
        {
            MyPlanet planet = entity as MyPlanet;
            if (planet == null)
                return;

            try
            {
                DefinitionDocument generatorDocument = null;
                if (planet.Generator != null)
                {
                    definitions.TryGet(planet.Generator.Id, out generatorDocument);
                    if (generatorDocument == null && unresolvedGeneratorIds.Add(planet.Generator.Id))
                        Warn("Planet generator definition " + planet.Generator.Id + " is unavailable.");
                }

                string name = !string.IsNullOrWhiteSpace(planet.DisplayNameText)
                    ? planet.DisplayNameText
                    : (!string.IsNullOrWhiteSpace(planet.Name) ? planet.Name : "Planet " + planet.EntityId);

                PlanetSnapshot snapshot = new PlanetSnapshot(
                    planet.EntityId,
                    name,
                    planet.PositionComp.GetPosition(),
                    planet.MinimumRadius,
                    planet.AverageRadius,
                    planet.MaximumRadius,
                    planet.HasAtmosphere,
                    planet.AtmosphereRadius,
                    planet.AtmosphereAltitude,
                    planet.Generator != null ? (VRage.Game.MyDefinitionId?)planet.Generator.Id : null,
                    generatorDocument != null ? generatorDocument.Origin : DefinitionOrigin.Unknown,
                    generatorDocument == null || generatorDocument.IsEnabled,
                    generatorDocument == null || generatorDocument.IsPublic,
                    generatorDocument == null || generatorDocument.IsAvailableInSurvival,
                    generatorDocument != null);

                planets[planet.EntityId] = snapshot;
                if (notify)
                    RaiseChanged();
            }
            catch (Exception exception)
            {
                Warn("Could not snapshot planet entity " + entity.EntityId + ": " + exception.Message);
            }
        }

        #endregion

        #region Event Dispatch and Ordering

        private void RaiseChanged()
        {
            Action handler = Changed;
            if (handler != null)
                handler();
        }

        private void Warn(string message)
        {
            if (logWarning != null)
                logWarning(message);
        }

        private static int ComparePlanets(PlanetSnapshot left, PlanetSnapshot right)
        {
            int name = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            return name != 0 ? name : left.EntityId.CompareTo(right.EntityId);
        }

        #endregion
    }
}
