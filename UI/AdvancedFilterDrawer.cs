using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRage.Game;
using VRageMath;

namespace SEpedia.UI
{
    public sealed class AdvancedFilterDrawer : HudElementBase
    {
        public event Action FiltersChanged;
        public event Action ResetRequested;

        private const int FacetPageSize = 8;

        private sealed class FacetSlot
        {
            public readonly NamedCheckBox CheckBox;
            public readonly ScrollBoxEntry Entry;
            public FacetCount Facet;

            public FacetSlot(NamedCheckBox checkBox, ScrollBoxEntry entry)
            {
                CheckBox = checkBox;
                Entry = entry;
            }
        }

        private sealed class FacetPage
        {
            private static readonly IReadOnlyList<FacetCount> EmptyFacets = new List<FacetCount>().AsReadOnly();

            private readonly AdvancedFilterDrawer owner;
            private readonly string headingText;
            private readonly HashSet<string> selected;
            private readonly bool showKeyToolTips;
            private readonly Label heading;
            private readonly NamedCheckBox all;
            private readonly ScrollBoxEntry headingEntry;
            private readonly ScrollBoxEntry allEntry;
            private readonly FacetSlot[] slots;
            private readonly LabelBoxButton previous;
            private readonly Label pageLabel;
            private readonly LabelBoxButton next;
            private readonly ScrollBoxEntry pagerEntry;
            private IReadOnlyList<FacetCount> facets;
            private int page;

            public FacetPage(
                AdvancedFilterDrawer owner,
                string headingText,
                string allText,
                HashSet<string> selected,
                bool showKeyToolTips,
                IList<ScrollBoxEntry> group = null)
            {
                this.owner = owner;
                this.headingText = headingText;
                this.selected = selected;
                this.showKeyToolTips = showKeyToolTips;
                facets = EmptyFacets;

                heading = CreateHeading(headingText);
                headingEntry = owner.AddRow(heading, group);

                all = CreateCheckBox(allText, true);
                all.MouseInput.LeftClicked += delegate
                {
                    if (owner.updating)
                        return;

                    if (all.Value)
                    {
                        selected.Clear();
                        owner.RaiseChanged();
                    }
                    else
                    {
                        all.Value = true;
                    }
                };
                allEntry = owner.AddRow(all, group);

                slots = new FacetSlot[FacetPageSize];
                for (int index = 0; index < slots.Length; index++)
                {
                    NamedCheckBox checkBox = CreateCheckBox(string.Empty, false);
                    ScrollBoxEntry entry = owner.AddRow(checkBox, group);
                    FacetSlot slot = new FacetSlot(checkBox, entry);
                    slots[index] = slot;

                    FacetSlot capturedSlot = slot;
                    checkBox.MouseInput.LeftClicked += delegate
                    {
                        if (owner.updating || capturedSlot.Facet == null)
                            return;

                        if (capturedSlot.CheckBox.Value)
                            selected.Add(capturedSlot.Facet.Key);
                        else
                            selected.Remove(capturedSlot.Facet.Key);

                        owner.RaiseChanged();
                    };
                }

                previous = CreatePagerButton("<", "Previous page");
                pageLabel = new Label
                {
                    Text = new RichText("1 / 1", GlyphFormat.Blueish.WithAlignment(TextAlignment.Center).WithSize(.72f)),
                    Height = 27f,
                    AutoResize = false,
                    VertCenterText = true
                };
                next = CreatePagerButton(">", "Next page");

                previous.MouseInput.LeftClicked += delegate
                {
                    if (page > 0)
                    {
                        page--;
                        UpdateVisibleSlots();
                    }
                };
                next.MouseInput.LeftClicked += delegate
                {
                    if (page < GetPageCount() - 1)
                    {
                        page++;
                        UpdateVisibleSlots();
                    }
                };

                var pager = new HudChain(false)
                {
                    Height = 27f,
                    SizingMode = HudChainSizingModes.FitMembersOffAxis,
                    Spacing = 4f,
                    CollectionContainer = { previous, { pageLabel, 1f }, next }
                };
                pagerEntry = owner.AddRow(pager, group);
            }

            public void Update(IReadOnlyList<FacetCount> newFacets, bool enabled)
            {
                facets = newFacets ?? EmptyFacets;
                int pageCount = GetPageCount();
                page = Math.Max(0, Math.Min(page, pageCount - 1));
                SetEnabled(enabled);
                UpdateVisibleSlots();
            }

            public void SetEnabled(bool enabled)
            {
                headingEntry.Enabled = enabled;
                allEntry.Enabled = enabled;

                for (int index = 0; index < slots.Length; index++)
                    slots[index].Entry.Enabled = enabled && slots[index].Facet != null;

                pagerEntry.Enabled = enabled && GetPageCount() > 1;
            }

            private void UpdateVisibleSlots()
            {
                bool pageEnabled = headingEntry.Enabled;
                int pageCount = GetPageCount();
                int start = page * FacetPageSize;

                heading.Text = new RichText(
                    selected.Count == 0
                        ? headingText
                        : headingText + " (" + selected.Count + " selected)",
                    GlyphFormat.Blueish.WithSize(.88f));
                all.Value = selected.Count == 0;

                for (int index = 0; index < slots.Length; index++)
                {
                    FacetSlot slot = slots[index];
                    int facetIndex = start + index;
                    slot.Facet = facetIndex < facets.Count ? facets[facetIndex] : null;
                    slot.Entry.Enabled = pageEnabled && slot.Facet != null;

                    if (slot.Facet == null)
                    {
                        slot.CheckBox.Name = new RichText(string.Empty, GlyphFormat.White.WithSize(.72f));
                        slot.CheckBox.MouseInput.ToolTip = null;
                        slot.CheckBox.Value = false;
                        continue;
                    }

                    slot.CheckBox.Name = new RichText(
                        slot.Facet.DisplayName + " (" + slot.Facet.Count + ")",
                        GlyphFormat.White.WithSize(.72f));
                    slot.CheckBox.Value = selected.Contains(slot.Facet.Key);
                    slot.CheckBox.MouseInput.ToolTip = showKeyToolTips ? slot.Facet.Key : null;
                }

                pageLabel.Text = new RichText(
                    (page + 1) + " / " + pageCount,
                    GlyphFormat.Blueish.WithAlignment(TextAlignment.Center).WithSize(.72f));
                previous.InputEnabled = page > 0;
                next.InputEnabled = page < pageCount - 1;
                previous.Color = previous.InputEnabled ? new Color(36, 47, 55) : new Color(28, 35, 40);
                next.Color = next.InputEnabled ? new Color(36, 47, 55) : new Color(28, 35, 40);
                pagerEntry.Enabled = pageEnabled && pageCount > 1;
            }

            private int GetPageCount()
            {
                return Math.Max(1, (facets.Count + FacetPageSize - 1) / FacetPageSize);
            }

            private static LabelBoxButton CreatePagerButton(string text, string toolTip)
            {
                var button = new LabelBoxButton
                {
                    Text = new RichText(text, GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.8f)),
                    Height = 27f,
                    Width = 42f,
                    AutoResize = false,
                    VertCenterText = true,
                    TextPadding = Vector2.Zero,
                    Color = new Color(36, 47, 55),
                    HighlightColor = new Color(67, 82, 92)
                };
                button.MouseInput.ToolTip = toolTip;
                return button;
            }
        }

        private readonly CatalogFilter filter;
        private readonly ScrollBox content;
        private readonly Dropdown<TriStateFilter> enabledFilter;
        private readonly Dropdown<TriStateFilter> publicFilter;
        private readonly Dropdown<TriStateFilter> survivalFilter;
        private readonly Dropdown<TriStateFilter> buildMenuFilter;
        private readonly NamedCheckBox smallGrid;
        private readonly NamedCheckBox largeGrid;
        private readonly List<ScrollBoxEntry> blockOnlyEntries;
        private readonly FacetPage blockTypes;
        private readonly FacetPage sources;
        private bool updating;

        public AdvancedFilterDrawer(CatalogFilter filter, HudParentBase parent = null) : base(parent)
        {
            this.filter = filter;
            blockOnlyEntries = new List<ScrollBoxEntry>();
            Width = 300f;

            content = new ScrollBox(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Padding = new Vector2(7f),
                Spacing = 3f,
                UseSmoothScrolling = true
            };

            var reset = new LabelBoxButton
            {
                Text = new RichText("X", GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.9f)),
                Height = 30f,
                Width = 30f,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = Vector2.Zero,
                Color = new Color(70, 45, 45),
                HighlightColor = new Color(110, 58, 58)
            };
            reset.MouseInput.ToolTip = "Reset advanced filters";
            reset.MouseInput.LeftClicked += delegate
            {
                Action handler = ResetRequested;
                if (handler != null)
                    handler();
            };

            var definitionHeading = new Label
            {
                Text = new RichText("Definition flags", GlyphFormat.Blueish.WithSize(.88f)),
                Height = 30f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(4f, 0f)
            };
            var definitionHeader = new HudChain(false)
            {
                Height = 30f,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 4f
            };
            definitionHeader.Add(definitionHeading, 1f);
            definitionHeader.Add(reset);
            AddRow(definitionHeader);

            enabledFilter = AddTriState("Enabled", delegate(TriStateFilter value) { filter.Enabled = value; });
            publicFilter = AddTriState("Public", delegate(TriStateFilter value) { filter.Public = value; });
            survivalFilter = AddTriState("Survival", delegate(TriStateFilter value) { filter.AvailableInSurvival = value; });

            buildMenuFilter = AddTriState("Listed in G menu", delegate(TriStateFilter value) { filter.ListedInBuildMenu = value; }, blockOnlyEntries);
            AddRow(CreateHeading("Grid size"), blockOnlyEntries);
            smallGrid = AddGridSize("Small", MyCubeSize.Small);
            largeGrid = AddGridSize("Large", MyCubeSize.Large);

            blockTypes = new FacetPage(
                this,
                "Runtime block type",
                "All block types",
                filter.SelectedBlockTypes,
                true,
                blockOnlyEntries);

            sources = new FacetPage(
                this,
                "Source",
                "All sources",
                filter.SelectedSourceKeys,
                false);
        }

        public void Refresh(CatalogResult result)
        {
            if (result == null)
                return;

            updating = true;
            try
            {
                enabledFilter.SetSelectionAt((int)filter.Enabled);
                publicFilter.SetSelectionAt((int)filter.Public);
                survivalFilter.SetSelectionAt((int)filter.AvailableInSurvival);
                buildMenuFilter.SetSelectionAt((int)filter.ListedInBuildMenu);
                smallGrid.Value = filter.SelectedGridSizes.Contains(MyCubeSize.Small);
                largeGrid.Value = filter.SelectedGridSizes.Contains(MyCubeSize.Large);

                bool showBlockFilters = filter.Category == BrowseCategory.Blocks;
                for (int index = 0; index < blockOnlyEntries.Count; index++)
                    blockOnlyEntries[index].Enabled = showBlockFilters;

                if (showBlockFilters)
                    blockTypes.Update(result.BlockTypes, true);
                else
                    blockTypes.SetEnabled(false);

                sources.Update(result.Sources, true);
            }
            finally
            {
                updating = false;
            }
        }

        private Dropdown<TriStateFilter> AddTriState(
            string name,
            Action<TriStateFilter> setValue,
            IList<ScrollBoxEntry> group = null)
        {
            var dropdown = new Dropdown<TriStateFilter>
            {
                Height = 34f,
                Format = GlyphFormat.White.WithSize(.76f),
                MemberPadding = new Vector2(8f, 2f),
                LineHeight = 25f,
                DropdownHeight = 78f
            };
            dropdown.Add(new RichText(name + ": Either", GlyphFormat.White.WithSize(.76f)), TriStateFilter.Either);
            dropdown.Add(new RichText(name + ": Yes", GlyphFormat.White.WithSize(.76f)), TriStateFilter.Yes);
            dropdown.Add(new RichText(name + ": No", GlyphFormat.White.WithSize(.76f)), TriStateFilter.No);
            dropdown.ValueChanged += delegate
            {
                if (!updating && dropdown.Value != null)
                {
                    setValue(dropdown.Value.AssocMember);
                    RaiseChanged();
                }
            };
            AddRow(dropdown, group);
            return dropdown;
        }

        private NamedCheckBox AddGridSize(string name, MyCubeSize size)
        {
            NamedCheckBox checkBox = CreateCheckBox(name, filter.SelectedGridSizes.Contains(size));
            checkBox.MouseInput.LeftClicked += delegate
            {
                if (updating)
                    return;

                if (checkBox.Value)
                    filter.SelectedGridSizes.Add(size);
                else if (filter.SelectedGridSizes.Count > 1)
                    filter.SelectedGridSizes.Remove(size);
                else
                {
                    checkBox.Value = true;
                    return;
                }

                RaiseChanged();
            };
            AddRow(checkBox, blockOnlyEntries);
            return checkBox;
        }

        private static NamedCheckBox CreateCheckBox(string name, bool value)
        {
            return new NamedCheckBox
            {
                Name = new RichText(name, GlyphFormat.White.WithSize(.72f)),
                Height = 27f,
                AutoResize = false,
                VertCenterText = true,
                Value = value
            };
        }

        private static Label CreateHeading(string text)
        {
            return new Label
            {
                Text = new RichText(text, GlyphFormat.Blueish.WithSize(.88f)),
                Height = 28f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(4f, 4f)
            };
        }

        private ScrollBoxEntry AddRow(HudElementBase row, IList<ScrollBoxEntry> group = null)
        {
            var entry = new ScrollBoxEntry();
            entry.SetElement(row);
            content.Add(entry);
            if (group != null)
                group.Add(entry);
            return entry;
        }

        private void RaiseChanged()
        {
            Action handler = FiltersChanged;
            if (handler != null)
                handler();
        }
    }
}
