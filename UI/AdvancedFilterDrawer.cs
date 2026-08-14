using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRage.Game;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class AdvancedFilterDrawer : HudElementBase
    {
        #region State

        public event Action FiltersChanged;
        public event Action ResetRequested;

        private readonly CatalogFilter filter;
        private readonly ScrollBox content;
        private readonly Dropdown<TriStateFilter> enabledFilter;
        private readonly Dropdown<TriStateFilter> publicFilter;
        private readonly Dropdown<TriStateFilter> survivalFilter;
        private readonly Dropdown<TriStateFilter> buildMenuFilter;
        private readonly NamedCheckBox smallGrid;
        private readonly NamedCheckBox largeGrid;
        private readonly List<ScrollBoxEntry> blockOnlyEntries;
        private readonly PagedFacetSection blockTypes;
        private readonly PagedFacetSection sources;
        private bool updating;

        #endregion

        #region Construction

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
                Color = UiTheme.Danger,
                HighlightColor = UiTheme.DangerHighlight
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

            enabledFilter = AddTriState("Enabled", delegate(TriStateFilter value) { filter.EnabledState = value; });
            publicFilter = AddTriState("Public", delegate(TriStateFilter value) { filter.PublicState = value; });
            survivalFilter = AddTriState("Survival", delegate(TriStateFilter value) { filter.SurvivalState = value; });

            buildMenuFilter = AddTriState("Listed in G menu", delegate(TriStateFilter value) { filter.BuildMenuState = value; }, blockOnlyEntries);
            AddRow(CreateHeading("Grid size"), blockOnlyEntries);
            smallGrid = AddGridSize("Small", MyCubeSize.Small);
            largeGrid = AddGridSize("Large", MyCubeSize.Large);

            blockTypes = new PagedFacetSection(
                content,
                "Runtime block type",
                "All block types",
                filter.SelectedBlockTypes,
                true,
                RaiseChanged,
                blockOnlyEntries);

            sources = new PagedFacetSection(
                content,
                "Source",
                "All sources",
                filter.SelectedSourceKeys,
                false,
                RaiseChanged);
        }

        #endregion

        #region Filter Synchronization

        public void Refresh(CatalogResult result)
        {
            if (result == null)
                return;

            updating = true;
            try
            {
                enabledFilter.SetSelectionAt((int)filter.EnabledState);
                publicFilter.SetSelectionAt((int)filter.PublicState);
                survivalFilter.SetSelectionAt((int)filter.SurvivalState);
                buildMenuFilter.SetSelectionAt((int)filter.BuildMenuState);
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

        #endregion

        #region Control Construction

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

        #endregion

        #region Event Dispatch

        private void RaiseChanged()
        {
            Action handler = FiltersChanged;
            if (handler != null)
                handler();
        }

        #endregion
    }
}
