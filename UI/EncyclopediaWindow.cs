using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class EncyclopediaWindow : WindowBase
    {
        #region State

        private readonly bool survivalMode;
        private readonly CelestialIndex celestial;
        private readonly TextField searchField;
        private readonly LabelBoxButton closeButton;
        private readonly DefinitionList definitionList;
        private readonly AdvancedFilterDrawer filterDrawer;
        private readonly DefinitionView definitionView;
        private readonly NavigationController navigation;
        private readonly VanillaHudVisibilityController vanillaHud;
        private bool closed;

        #endregion

        #region Construction

        public EncyclopediaWindow(
            DefinitionIndex index,
            CelestialIndex celestial,
            CatalogFilter filter,
            bool survivalMode,
            HudParentBase parent = null) : base(parent)
        {
            this.survivalMode = survivalMode;
            this.celestial = celestial;

            HeaderText = new RichText("SEpedia", GlyphFormat.White.WithAlignment(TextAlignment.Left).WithSize(1.08f));
            header.TextPadding = new Vector2(14f, 0f);

            searchField = new TextField(header)
            {
                Text = string.Empty,
                Width = 310f,
                Height = 25f,
                ParentAlignment = ParentAlignments.InnerRight,
                Offset = new Vector2(-31f, 0f),
                AutoResize = false,
                Format = GlyphFormat.White.WithSize(.82f),
                UpdateValueCallback = SearchChanged
            };
            // Keep the draggable header from taking focus back after the text field handles the click.
            ((MouseInputElement)searchField.MouseInput).ShareCursor = false;

            closeButton = new LabelBoxButton(header)
            {
                Text = new RichText("X", GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.9f)),
                Width = 29f,
                Height = 29f,
                ParentAlignment = ParentAlignments.InnerRight,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = Vector2.Zero,
                Color = new Color(0, 0, 0, 0),
                HighlightColor = new Color(0, 0, 0, 0),
                HighlightEnabled = false
            };
            closeButton.MouseInput.ToolTip = "Close";

            definitionList = new DefinitionList(index, filter, celestial != null ? celestial.Planets : null, survivalMode)
            {
                Width = 252f
            };

            filterDrawer = new AdvancedFilterDrawer(filter)
            {
                Width = 290f,
                Visible = false
            };

            definitionView = new DefinitionView(index);
            vanillaHud = new VanillaHudVisibilityController();
            closeButton.MouseInput.LeftClicked += delegate { Hide(); };

            var content = new HudChain(false)
            {
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f,
                CollectionContainer = { definitionList, filterDrawer, { definitionView, 1f } }
            };

            new HudChain(true, body)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f,
                CollectionContainer = { definitionList.CategoryBar, { content, 1f } }
            };

            navigation = new NavigationController(index, definitionList, definitionView);
            definitionList.FilterRequested += ToggleFilters;
            definitionList.ResetFiltersRequested += ResetFilters;
            definitionList.ResultsChanged += RefreshFilterDrawer;
            filterDrawer.FiltersChanged += FiltersChanged;

            BodyColor = new Color(31, 40, 47, 245);
            BorderColor = new Color(58, 68, 77);
            MinimumSize = new Vector2(840f, 520f);
            Size = new Vector2(1180f, 720f);
            MouseInput.RequestCursor = true;
            Visible = false;

            if (definitionList.First != null)
                definitionView.Show(definitionList.First);
        }

        #endregion

        #region Window Lifecycle

        public void Toggle()
        {
            if (closed)
                return;
            if (Visible)
            {
                Hide();
                return;
            }

            Visible = true;
            try
            {
                vanillaHud.Hide();
                GetWindowFocus();
            }
            catch
            {
                vanillaHud.Restore();
                Visible = false;
                throw;
            }
        }

        private void Hide()
        {
            searchField.CloseInput();
            Visible = false;
            vanillaHud.Restore();
        }

        public void RefreshCelestial()
        {
            definitionList.RebuildCatalog(celestial != null ? celestial.Planets : null);
        }

        public void Close()
        {
            if (closed)
                return;
            closed = true;

            searchField.CloseInput();
            Visible = false;
            vanillaHud.Restore();
            filterDrawer.FiltersChanged -= FiltersChanged;
            definitionList.ResultsChanged -= RefreshFilterDrawer;
            definitionList.ResetFiltersRequested -= ResetFilters;
            definitionList.FilterRequested -= ToggleFilters;
            navigation.Close();
            Unregister();
        }

        #endregion

        #region Layout

        protected override void Layout()
        {
            base.Layout();
            SetWidthIfChanged(searchField, Math.Min(360f, Math.Max(220f, Width * .32f)));
            SetWidthIfChanged(definitionList, Math.Min(280f, Math.Max(220f, body.Width * .224f)));
            SetWidthIfChanged(filterDrawer, Math.Min(320f, Math.Max(250f, body.Width * .25f)));
            definitionList.UpdateCategoryLayout(body.Width);
        }

        private static void SetWidthIfChanged(HudElementBase element, float width)
        {
            if (Math.Abs(element.Width - width) >= .01f)
                element.Width = width;
        }

        #endregion

        #region Search and Filter Events

        private void SearchChanged(object sender, EventArgs args)
        {
            definitionList.SetSearchText(searchField.Value != null ? searchField.Value.ToString() : string.Empty);
        }

        private void ToggleFilters()
        {
            filterDrawer.Visible = !filterDrawer.Visible;
            if (filterDrawer.Visible)
                RefreshFilterDrawer();
        }

        private void FiltersChanged()
        {
            definitionList.Refresh();
        }

        private void ResetFilters()
        {
            definitionList.Filter.ResetAdvanced(survivalMode);
            definitionList.Refresh();
        }

        private void RefreshFilterDrawer()
        {
            if (filterDrawer.Visible && definitionList.CurrentResults != null)
                filterDrawer.Refresh(definitionList.CurrentResults);
        }

        #endregion
    }
}
