using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DetailScrollBox : ScrollBox
    {
        private const float WheelStep = 48f;
        private float retainedValue;
        private bool restorePending;
        private bool wheelInputArmed;

        public DetailScrollBox(HudParentBase parent = null) : base(parent)
        {
        }

        #region Scrolling

        public void ResetScroll()
        {
            retainedValue = 0f;
            restorePending = false;
            wheelInputArmed = false;
            ScrollBar.Value = 0f;
        }

        protected override void HandleInput(Vector2 cursorPos)
        {
            bool inputAvailable = EnableScrolling && HudMain.InputMode != HudInputMode.NoInput;
            ScrollBar.InputEnabled = inputAvailable;
            ShareCursor = ScrollBar.Max <= 0f;

            bool wheelUp = SharedBinds.MousewheelUp.IsPressed;
            bool wheelDown = SharedBinds.MousewheelDown.IsPressed;
            if (!inputAvailable || SharedBinds.Alt.IsPressed)
            {
                wheelInputArmed = false;
                return;
            }

            if (!wheelUp && !wheelDown)
            {
                wheelInputArmed = true;
                return;
            }

            if (!wheelInputArmed || Count == 0 || !(IsMousedOver || ScrollBar.IsMousedOver))
                return;

            if (wheelUp)
                SetScrollValue(ScrollBar.Value - WheelStep);
            else if (wheelDown)
                SetScrollValue(ScrollBar.Value + WheelStep);
        }

        private void SetScrollValue(float value)
        {
            retainedValue = MathHelper.Clamp(value, 0f, ScrollBar.Max);
            restorePending = false;
            ScrollBar.Value = retainedValue;
        }

        #endregion

        #region Offset Retention

        protected override void Layout()
        {
            if (HudMain.InputMode == HudInputMode.NoInput)
                wheelInputArmed = false;

            float previousMax = ScrollBar.Max;
            if (previousMax > 0f && !restorePending)
                retainedValue = ScrollBar.Value;

            base.Layout();

            if (ScrollBar.Max <= 0f)
            {
                if (previousMax > 0f && retainedValue > 0f)
                    restorePending = true;
                return;
            }

            if (restorePending)
            {
                ScrollBar.Value = Math.Min(retainedValue, ScrollBar.Max);
                restorePending = false;
            }
            else
                retainedValue = ScrollBar.Value;
        }

        #endregion
    }
}
