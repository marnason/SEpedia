using System;
using Sandbox.Game;
using Sandbox.ModAPI;
using SEpedia.Core;

namespace SEpedia.UI
{
    internal sealed class VanillaHudVisibilityController
    {
        private int previousHudState;
        private long playerIdentityId;
        private bool hidden;

        public void Hide()
        {
            if (hidden)
                return;

            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.Config == null ||
                    MyAPIGateway.Session.Player == null)
                    return;

                previousHudState = MyAPIGateway.Session.Config.HudState;
                playerIdentityId = MyAPIGateway.Session.Player.IdentityId;
                MyVisualScriptLogicProvider.SetHudState(0, playerIdentityId);
                hidden = true;
            }
            catch (Exception exception)
            {
                SEpediaLog.Warning("Could not temporarily hide the vanilla HUD: " + exception.Message);
            }
        }

        public void Restore()
        {
            if (!hidden)
                return;

            hidden = false;

            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.Config == null ||
                    MyAPIGateway.Session.Player == null ||
                    MyAPIGateway.Session.Player.IdentityId != playerIdentityId)
                    return;

                // Do not overwrite a HUD change made by the player or another mod while SEpedia was open.
                if (MyAPIGateway.Session.Config.HudState == 0)
                    MyVisualScriptLogicProvider.SetHudState(previousHudState, playerIdentityId);
            }
            catch (Exception exception)
            {
                SEpediaLog.Warning("Could not restore the vanilla HUD state: " + exception.Message);
            }
        }
    }
}
