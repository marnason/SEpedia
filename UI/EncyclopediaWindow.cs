using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    public sealed class EncyclopediaWindow : WindowBase
    {
        private readonly bool survivalMode;
        private readonly CelestialIndex celestial;
        private readonly TextField searchField;
        private readonly DefinitionList definitionList;
        private readonly AdvancedFilterDrawer filterDrawer;
        private readonly DefinitionView definitionView;
        private readonly NavigationController navigation;
        private readonly VanillaHudVisibilityController vanillaHud;

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
                AutoResize = false,
                Format = GlyphFormat.White.WithSize(.82f),
                UpdateValueCallback = SearchChanged
            };

            definitionList = new DefinitionList(index, filter, celestial != null ? celestial.Planets : null)
            {
                Width = 315f
            };

            filterDrawer = new AdvancedFilterDrawer(filter)
            {
                Width = 290f,
                Visible = false
            };

            definitionView = new DefinitionView(index);
            vanillaHud = new VanillaHudVisibilityController();

            new HudChain(false, body)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f,
                CollectionContainer = { definitionList, filterDrawer, { definitionView, 1f } }
            };

            navigation = new NavigationController(index, definitionList, definitionView);
            definitionList.FilterRequested += ToggleFilters;
            definitionList.ResultsChanged += RefreshFilterDrawer;
            filterDrawer.FiltersChanged += FiltersChanged;
            filterDrawer.ResetRequested += ResetFilters;
            RefreshFilterDrawer();

            BodyColor = new Color(31, 40, 47, 245);
            BorderColor = new Color(58, 68, 77);
            MinimumSize = new Vector2(840f, 520f);
            Size = new Vector2(1180f, 720f);
            MouseInput.RequestCursor = true;
            Visible = false;

            if (definitionList.First != null)
                definitionView.Show(definitionList.First);
        }

        public void Toggle()
        {
            Visible = !Visible;

            if (Visible)
            {
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
            else
            {
                searchField.CloseInput();
                vanillaHud.Restore();
            }
        }

        public void RefreshCelestial()
        {
            definitionList.RebuildCatalog(celestial != null ? celestial.Planets : null);
            RefreshFilterDrawer();
        }

        public void Close()
        {
            vanillaHud.Restore();
            definitionList.FilterRequested -= ToggleFilters;
            definitionList.ResultsChanged -= RefreshFilterDrawer;
            filterDrawer.FiltersChanged -= FiltersChanged;
            filterDrawer.ResetRequested -= ResetFilters;
            navigation.Close();
            Unregister();
        }

        protected override void Layout()
        {
            base.Layout();
            searchField.Width = Math.Min(360f, Math.Max(220f, Width * .32f));
            definitionList.Width = Math.Min(350f, Math.Max(275f, body.Width * .28f));
            filterDrawer.Width = Math.Min(320f, Math.Max(250f, body.Width * .25f));
        }

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
            if (definitionList.CurrentResults != null)
                filterDrawer.Refresh(definitionList.CurrentResults);
        }
    }
}
