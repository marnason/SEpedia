using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DefinitionList : HudElementBase
    {
        #region State

        public event Action<CatalogEntry, bool> SelectionChanged;
        public event Action FilterRequested;
        public event Action ResetFiltersRequested;
        public event Action ResultsChanged;

        private readonly DefinitionIndex definitions;
        private readonly CatalogFilter filter;
        private readonly bool survivalMode;
        private readonly CategoryBar categoryBar;
        private readonly ListBox<CatalogEntry> list;
        private readonly PagerRow pager;
        private readonly Label status;
        private readonly Label activeFilterStatus;
        private readonly LabelBoxButton resetFiltersButton;
        private CatalogIndex catalog;
        private CatalogResult currentResults;
        private DefinitionDocument includedDefinition;
        private int pendingRevealIndex;
        private bool revealLayoutReady;
        private bool updating;

        public CatalogEntry First
        {
            get
            {
                return currentResults != null && currentResults.Items.Count > 0
                    ? currentResults.Items[0]
                    : null;
            }
        }

        public CatalogFilter Filter
        {
            get { return filter; }
        }

        public CatalogResult CurrentResults
        {
            get { return currentResults; }
        }

        public HudElementBase CategoryBar
        {
            get { return categoryBar.Root; }
        }

        #endregion

        #region Construction

        public DefinitionList(
            DefinitionIndex definitions,
            CatalogFilter filter,
            IEnumerable<PlanetSnapshot> planets,
            bool survivalMode,
            HudParentBase parent = null) : base(parent)
        {
            this.definitions = definitions;
            this.filter = filter;
            this.survivalMode = survivalMode;
            catalog = new CatalogIndex(definitions, planets);
            pendingRevealIndex = -1;
            revealLayoutReady = false;
            categoryBar = new CategoryBar(filter, Refresh);

            var filterButton = new LabelBoxButton
            {
                Text = new RichText("Advanced filters", GlyphFormat.White.WithSize(.8f)),
                Height = 29f,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = new Vector2(16f, 0f),
                Color = UiTheme.Panel,
                HighlightColor = UiTheme.PanelHighlight
            };
            filterButton.MouseInput.LeftClicked += delegate
            {
                Action handler = FilterRequested;
                if (handler != null)
                    handler();
            };

            activeFilterStatus = new Label
            {
                Width = 108f,
                Height = 29f,
                AutoResize = false,
                VertCenterText = true,
                Format = GlyphFormat.Blueish.WithAlignment(TextAlignment.Right).WithSize(.66f),
                Padding = new Vector2(6f, 0f),
                Visible = false
            };

            resetFiltersButton = new LabelBoxButton
            {
                Text = new RichText("X", GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.9f)),
                Height = 29f,
                Width = 29f,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = Vector2.Zero,
                Color = UiTheme.Danger,
                HighlightColor = UiTheme.DangerHighlight,
                Visible = false
            };
            resetFiltersButton.MouseInput.ToolTip = "Reset filters";
            resetFiltersButton.MouseInput.LeftClicked += delegate
            {
                Action handler = ResetFiltersRequested;
                if (handler != null)
                    handler();
            };

            var filterRow = new HudChain(false)
            {
                Height = 29f,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f
            };
            filterRow.Add(filterButton, 1f);
            filterRow.Add(activeFilterStatus);
            filterRow.Add(resetFiltersButton);

            list = new ListBox<CatalogEntry>
            {
                DimAlignment = DimAlignments.Width,
                Format = GlyphFormat.White.WithSize(.85f),
                HighlightPadding = new Vector2(2f, 0f),
                LineHeight = 27f,
                MemberPadding = new Vector2(24f, 4f),
                UpdateValueCallback = OnSelectionChanged
            };
            UiTheme.StyleVerticalScrollBar(list.EntryChain.ScrollBar);

            pager = new PagerRow(RefreshPage);

            status = new Label
            {
                Height = 24f,
                AutoResize = false,
                VertCenterText = true,
                Format = GlyphFormat.Blueish.WithSize(.72f),
                Padding = new Vector2(8f, 0f)
            };

            new HudChain(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f,
                CollectionContainer = { filterRow, { list, 1f }, pager.Root, status }
            };

            RefreshCategoryAvailability();
            Refresh();
        }

        #endregion

        #region Catalog Queries and Rendering

        public void SetSearchText(string query)
        {
            filter.SearchText = query ?? string.Empty;
            Refresh();
        }

        public void Refresh()
        {
            includedDefinition = null;
            pager.Reset();
            RefreshResults();
        }

        private void RefreshPage()
        {
            RefreshResults();
        }

        private void RefreshResults()
        {
            pendingRevealIndex = -1;
            revealLayoutReady = false;
            string previousKey = list.Value != null && list.Value.AssocMember != null
                ? list.Value.AssocMember.StableKey
                : string.Empty;

            filter.NormalizeForCategory();
            int offset = pager.Page * UiTheme.CatalogPageSize;
            currentResults = catalog.Query(
                filter,
                offset,
                UiTheme.CatalogPageSize,
                includedDefinition);
            if (includedDefinition == null &&
                filter.ReconcileAvailableFacets(
                    currentResults.Sources,
                    currentResults.BlockTypes,
                    currentResults.CelestialKinds))
                currentResults = catalog.Query(filter, offset, UiTheme.CatalogPageSize);
            pager.Configure(currentResults.TotalCount, UiTheme.CatalogPageSize);
            updating = true;
            try
            {
                list.ClearEntries();
                int selectedIndex = -1;
                for (int itemIndex = 0; itemIndex < currentResults.Items.Count; itemIndex++)
                {
                    CatalogEntry entry = currentResults.Items[itemIndex];
                    var text = new RichText();
                    text.Add(entry.DisplayName, GlyphFormat.White.WithSize(.83f));
                    list.Add(text, entry);
                    if (entry.StableKey == previousKey)
                        selectedIndex = itemIndex;
                }

                if (currentResults.Items.Count > 0)
                    list.SetSelectionAt(selectedIndex >= 0 ? selectedIndex : 0);
            }
            finally
            {
                updating = false;
            }
            ScrollToTop();

            string categoryName = CatalogText.GetCategoryName(filter.Category).ToLowerInvariant();
            int first = pager.Page * UiTheme.CatalogPageSize + 1;
            int last = pager.Page * UiTheme.CatalogPageSize + currentResults.Items.Count;
            status.Text = currentResults.TotalCount > UiTheme.CatalogPageSize
                ? "Showing " + first + "–" + last + " of " + currentResults.TotalCount + " " + categoryName
                : currentResults.TotalCount + " " + categoryName;
            UpdateActiveFilterStatus();

            if (list.Value != null)
                RaiseSelectionChanged(list.Value.AssocMember, false);

            Action resultsHandler = ResultsChanged;
            if (resultsHandler != null)
                resultsHandler();
        }

        public void RebuildCatalog(IEnumerable<PlanetSnapshot> planets)
        {
            catalog = new CatalogIndex(definitions, planets);
            RefreshCategoryAvailability();
            Refresh();
        }

        private void RefreshCategoryAvailability()
        {
            categoryBar.UpdateAvailability(delegate(BrowseCategory category)
            {
                return catalog.HasMultipleDefaultEntries(category, survivalMode);
            });
        }

        #endregion

        #region Layout and Selection

        protected override void Layout()
        {
            if (pendingRevealIndex < 0)
                return;

            if (!revealLayoutReady)
            {
                revealLayoutReady = true;
                return;
            }

            int revealIndex = pendingRevealIndex;
            pendingRevealIndex = -1;
            revealLayoutReady = false;
            CenterListOn(revealIndex);
        }

        private void CenterListOn(int targetIndex)
        {
            float targetCenter = 0f;
            for (int index = 0; index <= targetIndex && index < list.EntryList.Count; index++)
            {
                ListBoxEntry<CatalogEntry> entry = list.EntryList[index];
                if (!entry.Enabled)
                    continue;

                float rowHeight = entry.Element.UnpaddedSize.Y + entry.Element.Padding.Y;
                if (index == targetIndex)
                {
                    targetCenter += rowHeight * .5f;
                    break;
                }
                targetCenter += rowHeight + list.EntryChain.Spacing;
            }

            float centeredOffset = targetCenter - list.EntryChain.UnpaddedSize.Y * .5f;
            list.EntryChain.ScrollBar.Value = Math.Max(0f, centeredOffset);
        }

        private void ScrollToTop()
        {
            list.EntryChain.ScrollBar.Value = 0f;
            list.EntryChain.Start = 0;
        }

        private void UpdateActiveFilterStatus()
        {
            int count = filter.GetActiveAdvancedFilterCount();
            bool active = count > 0;
            activeFilterStatus.Visible = active;
            resetFiltersButton.Visible = active;
            if (active)
            {
                activeFilterStatus.Text = new RichText(
                    count + (count == 1 ? " filter active" : " filters active"),
                    activeFilterStatus.Format);
            }
        }

        public void UpdateCategoryLayout(float availableWidth)
        {
            categoryBar.UpdateLayout(availableWidth);
        }

        public bool TrySelect(DefinitionDocument definition)
        {
            if (definition == null)
                return false;

            for (int index = 0; index < list.EntryList.Count; index++)
            {
                CatalogEntry entry = list.EntryList[index].AssocMember;
                if (entry.Definition != null && entry.Definition.Id == definition.Id)
                    return TrySelectAt(index);
            }
            return false;
        }

        public bool TrySelect(CatalogEntry target)
        {
            if (target == null)
                return false;

            for (int index = 0; index < list.EntryList.Count; index++)
            {
                CatalogEntry entry = list.EntryList[index].AssocMember;
                if (entry.StableKey == target.StableKey)
                    return TrySelectAt(index);
            }
            return false;
        }

        private bool TrySelectAt(int index)
        {
            list.SetSelectionAt(index);
            pendingRevealIndex = index;
            revealLayoutReady = false;
            return true;
        }

        public bool TryReveal(DefinitionDocument definition)
        {
            includedDefinition = null;
            if (definition == null || definition.BrowseCategory == BrowseCategory.None)
                return false;

            filter.Category = definition.BrowseCategory;
            filter.NormalizeForCategory();
            categoryBar.UpdateSelection();
            includedDefinition = definition;

            int totalCount;
            int resultIndex = catalog.FindDefinitionResultIndex(
                filter,
                definition.Id,
                includedDefinition,
                out totalCount);
            if (resultIndex < 0)
            {
                includedDefinition = null;
                return false;
            }

            pager.Configure(totalCount, UiTheme.CatalogPageSize);
            pager.SetPage(resultIndex / UiTheme.CatalogPageSize);
            RefreshResults();
            return TrySelect(definition);
        }

        #endregion

        #region Event Dispatch

        private void OnSelectionChanged(object sender, EventArgs args)
        {
            if (!updating && list.Value != null)
                RaiseSelectionChanged(list.Value.AssocMember, true);
        }

        private void RaiseSelectionChanged(CatalogEntry entry, bool explicitSelection)
        {
            Action<CatalogEntry, bool> handler = SelectionChanged;
            if (handler != null)
                handler(entry, explicitSelection);
        }

        #endregion
    }
}
