using System;
using RichHudFramework.UI.Client;
using SEpedia.Core;

namespace SEpedia.UI
{
    internal sealed class SEpediaFrontend
    {
        private BindingConfigController bindings;
        private EncyclopediaWindow window;
        private DefinitionIndex index;
        private CelestialIndex celestial;
        private CatalogFilter filter;
        private bool survivalMode;
        private bool pendingOpen;
        private bool closed;

        public void InitializeRichHud()
        {
            closed = false;
            CloseRichHudObjects(false);

            try
            {
                bindings = new BindingConfigController();
                bindings.ToggleRequested += OnToggleRequested;
                TryCreateWindow();
                SEpediaLog.Info("Rich HUD interface initialized.");
            }
            catch (Exception exception)
            {
                CloseRichHudObjects(false);
                SEpediaLog.Error("Rich HUD interface initialization failed.", exception);
            }
        }

        public void AttachIndex(DefinitionIndex definitionIndex, CelestialIndex celestialIndex, bool isSurvivalMode)
        {
            index = definitionIndex;
            celestial = celestialIndex;
            survivalMode = isSurvivalMode;
            if (filter == null)
                filter = new CatalogFilter(survivalMode);
            TryCreateWindow();
        }

        public void RefreshCelestial()
        {
            if (window != null)
                window.RefreshCelestial();
        }

        public void PollBindings()
        {
            if (bindings != null)
                bindings.PollForChanges();
        }

        public void Save()
        {
            if (bindings != null)
                bindings.Save();
        }

        public void ResetRichHud()
        {
            CloseRichHudObjects(false);
        }

        public void Close()
        {
            if (closed)
                return;
            closed = true;
            CloseRichHudObjects(true);
            index = null;
            celestial = null;
            filter = null;
            pendingOpen = false;
        }

        private void TryCreateWindow()
        {
            if (window != null || bindings == null || index == null)
                return;

            try
            {
                window = new EncyclopediaWindow(index, celestial, filter, survivalMode, HudMain.HighDpiRoot);
                if (pendingOpen)
                {
                    pendingOpen = false;
                    window.Toggle();
                }
            }
            catch (Exception exception)
            {
                SEpediaLog.Error("Could not create the encyclopedia window.", exception);
            }
        }

        private void OnToggleRequested()
        {
            if (window != null)
                window.Toggle();
            else
                pendingOpen = true;
        }

        private void CloseRichHudObjects(bool saveBindings)
        {
            // Release dependants in reverse acquisition order: window first,
            // then the binding/settings registration that can open it.
            if (window != null)
            {
                try
                {
                    window.Close();
                }
                catch (Exception exception)
                {
                    SEpediaLog.Warning("Could not unregister the Rich HUD window cleanly: " + exception.Message);
                }

                window = null;
            }

            if (bindings != null)
            {
                bindings.ToggleRequested -= OnToggleRequested;
                bindings.Close(saveBindings);
                bindings = null;
            }
        }
    }
}
