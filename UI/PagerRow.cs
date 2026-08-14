using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class PagerRow
    {
        #region State and Construction

        private readonly LabelBoxButton previous;
        private readonly Label pageLabel;
        private readonly LabelBoxButton next;
        private readonly Action pageChanged;
        private int pageCount;

        public HudChain Root { get; private set; }
        public int Page { get; private set; }

        public PagerRow(Action pageChanged)
        {
            this.pageChanged = pageChanged;
            pageCount = 1;

            previous = CreateButton("<", "Previous page");
            pageLabel = new Label
            {
                Height = UiTheme.StandardRowHeight,
                AutoResize = false,
                VertCenterText = true
            };
            next = CreateButton(">", "Next page");

            previous.MouseInput.LeftClicked += delegate { Move(-1); };
            next.MouseInput.LeftClicked += delegate { Move(1); };

            Root = new HudChain(false)
            {
                Height = UiTheme.StandardRowHeight,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = UiTheme.ControlSpacing,
                CollectionContainer = { previous, { pageLabel, 1f }, next }
            };
            UpdateVisuals();
        }

        #endregion

        #region Paging

        public void Configure(int itemCount, int pageSize)
        {
            pageCount = Math.Max(1, (itemCount + pageSize - 1) / pageSize);
            Page = Math.Max(0, Math.Min(Page, pageCount - 1));
            UpdateVisuals();
        }

        private void Move(int delta)
        {
            int target = Math.Max(0, Math.Min(Page + delta, pageCount - 1));
            if (target == Page)
                return;

            Page = target;
            UpdateVisuals();
            if (pageChanged != null)
                pageChanged();
        }

        #endregion

        #region Rendering

        private void UpdateVisuals()
        {
            Root.Visible = pageCount > 1;
            pageLabel.Text = new RichText((Page + 1) + " / " + pageCount, UiTheme.PagerLabel);
            previous.InputEnabled = Page > 0;
            next.InputEnabled = Page < pageCount - 1;
            previous.Color = previous.InputEnabled ? UiTheme.Panel : UiTheme.Disabled;
            next.Color = next.InputEnabled ? UiTheme.Panel : UiTheme.Disabled;
        }

        #endregion

        #region Control Construction

        private static LabelBoxButton CreateButton(string text, string toolTip)
        {
            var button = new LabelBoxButton
            {
                Text = new RichText(text, UiTheme.PagerText),
                Height = UiTheme.StandardRowHeight,
                Width = UiTheme.PagerButtonWidth,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = Vector2.Zero,
                Color = UiTheme.Panel,
                HighlightColor = UiTheme.PanelHighlight
            };
            button.MouseInput.ToolTip = toolTip;
            return button;
        }

        #endregion
    }
}
