using System;
using System.Diagnostics;
using RichHudFramework.Client;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SEpedia.Core;
using SEpedia.UI;
using VRage.Game;
using VRage.Game.Components;

namespace SEpedia
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public sealed class SEpediaSession : MySessionComponentBase
    {
        private const int RetryDelayTicks = 300;
        private const int UiWarningDelayTicks = 600;
        private const int BindingPollTicks = 120;

        private DefinitionIndex definitionIndex;
        private CelestialIndex celestialIndex;
        private SEpediaFrontend frontend;
        private int tick;
        private int nextBuildAttempt;
        private bool uiWarningLogged;
        private bool unloading;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            tick = 0;
            nextBuildAttempt = 0;
            definitionIndex = null;
            celestialIndex = null;
            unloading = false;
            uiWarningLogged = false;

            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                frontend = new SEpediaFrontend();
                try
                {
                    RichHudClient.Init("SEpedia", OnRichHudReady, OnRichHudReset);
                }
                catch (Exception exception)
                {
                    SEpediaLog.Error("Could not begin Rich HUD registration.", exception);
                }
            }

            SEpediaLog.Info("Session initialized; waiting for runtime definitions.");
        }

        public override void UpdateAfterSimulation()
        {
            tick++;

            if (definitionIndex == null && tick >= nextBuildAttempt)
                TryBuildIndex();

            if (frontend != null && tick % BindingPollTicks == 0)
                frontend.PollBindings();

            if (!uiWarningLogged && frontend != null && tick >= UiWarningDelayTicks && !RichHudClient.Registered)
            {
                uiWarningLogged = true;
                SEpediaLog.Warning("Rich HUD Master is unavailable; the definition index remains active but SEpedia UI is disabled.");
            }
        }

        public override void SaveData()
        {
            if (frontend != null)
                frontend.Save();
        }

        protected override void UnloadData()
        {
            unloading = true;

            if (frontend != null)
            {
                frontend.Close();
                frontend = null;
            }

            if (celestialIndex != null)
            {
                celestialIndex.Changed -= OnCelestialChanged;
                celestialIndex.Close();
                celestialIndex = null;
            }

            definitionIndex = null;
            SEpediaLog.Info("Session unloaded.");
            base.UnloadData();
        }

        private void TryBuildIndex()
        {
            MyDefinitionManager manager = MyDefinitionManager.Static;
            if (manager == null || manager.Loading)
                return;

            var stopwatch = Stopwatch.StartNew();
            try
            {
                bool survivalMode = MyAPIGateway.Session != null && !MyAPIGateway.Session.CreativeMode;
                DefinitionIndex built = DefinitionIndexBuilder.Build(manager, survivalMode, SEpediaLog.Warning);
                stopwatch.Stop();
                definitionIndex = built;

                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    try
                    {
                        celestialIndex = new CelestialIndex(built, SEpediaLog.Warning);
                        celestialIndex.Changed += OnCelestialChanged;
                        celestialIndex.Initialize();
                    }
                    catch (Exception exception)
                    {
                        if (celestialIndex != null)
                        {
                            celestialIndex.Changed -= OnCelestialChanged;
                            celestialIndex.Close();
                            celestialIndex = null;
                        }
                        SEpediaLog.Error("Celestial entity tracking could not be initialized; definition browsing remains available.", exception);
                    }
                }

                SEpediaLog.Info(
                    "Indexed " + built.All.Count + " of " + built.SourceCount + " definitions, " +
                    built.Recipes.Count + " recipes (" + built.Recipes.MenuCount + " production-menu reachable), with " +
                    built.IssueCount + " isolated issues in " +
                    stopwatch.ElapsedMilliseconds + " ms.");
                SEpediaLog.Info(
                    "Icon resolution: " + built.IconStats.RenderableDefinitions + " of " +
                    built.IconStats.DefinitionsWithIcons + " definitions renderable through packaged aliases; " +
                    built.IconStats.UnresolvedDefinitions + " unresolved and " +
                    built.IconStats.LayerLimitDefinitions + " above the layer limit.");

                if (frontend != null)
                    frontend.AttachIndex(built, celestialIndex, survivalMode);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                nextBuildAttempt = tick + RetryDelayTicks;
                SEpediaLog.Error(
                    "Definition enumeration failed after " + stopwatch.ElapsedMilliseconds +
                    " ms; retrying in " + RetryDelayTicks + " simulation ticks.", exception);
            }
        }

        private void OnRichHudReady()
        {
            if (unloading || frontend == null)
                return;

            frontend.InitializeRichHud();
            if (definitionIndex != null)
                frontend.AttachIndex(
                    definitionIndex,
                    celestialIndex,
                    MyAPIGateway.Session != null && !MyAPIGateway.Session.CreativeMode);
        }

        private void OnCelestialChanged()
        {
            if (frontend != null)
                frontend.RefreshCelestial();
        }

        private void OnRichHudReset()
        {
            if (frontend != null)
                frontend.ResetRichHud();
        }
    }
}
