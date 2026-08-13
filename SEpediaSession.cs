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
                DefinitionIndex built = DefinitionIndex.Build(manager.GetAllDefinitions(), SEpediaLog.Warning);
                stopwatch.Stop();
                definitionIndex = built;

                SEpediaLog.Info(
                    "Indexed " + built.All.Count + " of " + built.SourceCount + " definitions, " +
                    built.Recipes.Count + " recipes, with " + built.IssueCount + " isolated issues in " +
                    stopwatch.ElapsedMilliseconds + " ms.");

                if (frontend != null)
                    frontend.AttachIndex(built);
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
                frontend.AttachIndex(definitionIndex);
        }

        private void OnRichHudReset()
        {
            if (frontend != null)
                frontend.ResetRichHud();
        }
    }
}
