using System;
using RichHudFramework.UI.Client;
using SEpedia.Core;

namespace SEpedia.UI
{
    public sealed class SEpediaFrontend
    {
        private BindingConfigController bindings;
        private EncyclopediaWindow window;
        private DefinitionIndex index;
        private bool pendingOpen;

        public void InitializeRichHud()
        {
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

        public void AttachIndex(DefinitionIndex definitionIndex)
        {
            index = definitionIndex;
            TryCreateWindow();
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
            CloseRichHudObjects(true);
            index = null;
            pendingOpen = false;
        }

        private void TryCreateWindow()
        {
            if (window != null || bindings == null || index == null)
                return;

            try
            {
                window = new EncyclopediaWindow(index, HudMain.HighDpiRoot);
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
            if (bindings != null)
            {
                bindings.ToggleRequested -= OnToggleRequested;
                bindings.Close(saveBindings);
                bindings = null;
            }

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
        }
    }
}
