using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace SEpedia.UI
{
    internal static class UiTheme
    {
        public const float StandardRowHeight = 27f;
        public const float PagerButtonWidth = 42f;
        public const float ControlSpacing = 4f;
        public const int AdvancedFilterPageSize = 8;
        public const int DetailSectionPageSize = 16;
        public const int CatalogPageSize = 500;
        public const float VerticalScrollBarWidth = 23f;

        public static readonly Color Panel = new Color(36, 47, 55);
        public static readonly Color PanelHighlight = new Color(67, 82, 92);
        public static readonly Color FilterSection = new Color(27, 36, 42);
        public static readonly Color FilterSectionBorder = new Color(58, 70, 79);
        public static readonly Color Disabled = new Color(28, 35, 40);
        public static readonly Color Selected = new Color(142, 188, 206);
        public static readonly Color SelectedText = new Color(39, 49, 55);
        public static readonly Color Danger = new Color(70, 45, 45);
        public static readonly Color DangerHighlight = new Color(110, 58, 58);

        public static GlyphFormat PagerText
        {
            get { return GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.8f); }
        }

        public static GlyphFormat PagerLabel
        {
            get { return GlyphFormat.Blueish.WithAlignment(TextAlignment.Center).WithSize(.72f); }
        }

        public static void StyleVerticalScrollBar(ScrollBar scrollBar)
        {
            scrollBar.Padding = new Vector2(10f);
            scrollBar.Width = VerticalScrollBarWidth;
            scrollBar.SlideInput.Offset = new Vector2(.5f, 0f);
        }
    }
}
