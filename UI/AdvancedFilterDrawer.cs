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
        private BrowseCategory lastCategory;
        private bool updating;

        #endregion

        #region Construction

        public AdvancedFilterDrawer(CatalogFilter filter, HudParentBase parent = null) : base(parent)
        {
            this.filter = filter;
            blockOnlyEntries = new List<ScrollBoxEntry>();
            lastCategory = filter.Category;
            Width = 300f;

            content = new ScrollBox(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Padding = new Vector2(7f),
                Spacing = 7f,
                UseSmoothScrolling = true
            };
            UiTheme.StyleVerticalScrollBar(content.ScrollBar);

            var definitionSection = new FilterSectionPanel(content);
            definitionSection.Add(CreateHeading("Definition flags"));

            enabledFilter = AddTriState(definitionSection, "Enabled", delegate(TriStateFilter value) { filter.EnabledState = value; });
            publicFilter = AddTriState(definitionSection, "Public", delegate(TriStateFilter value) { filter.PublicState = value; });
            survivalFilter = AddTriState(definitionSection, "Survival", delegate(TriStateFilter value) { filter.SurvivalState = value; });

            var availabilitySection = new FilterSectionPanel(content, blockOnlyEntries);
            availabilitySection.Add(CreateHeading("Block availability"));
            buildMenuFilter = AddTriState(availabilitySection, "Listed in G menu", delegate(TriStateFilter value) { filter.BuildMenuState = value; });

            var gridSection = new FilterSectionPanel(content, blockOnlyEntries);
            gridSection.Add(CreateHeading("Grid size"));
            smallGrid = AddGridSize(gridSection, "Small", MyCubeSize.Small);
            largeGrid = AddGridSize(gridSection, "Large", MyCubeSize.Large);

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

            bool categoryChanged = lastCategory != filter.Category;
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

            if (categoryChanged)
            {
                content.ScrollBar.Value = 0f;
                content.Start = 0;
                lastCategory = filter.Category;
            }
        }

        #endregion

        #region Control Construction

        private Dropdown<TriStateFilter> AddTriState(
            FilterSectionPanel section,
            string name,
            Action<TriStateFilter> setValue)
        {
            var label = new Label
            {
                Text = new RichText(name, GlyphFormat.White.WithSize(.76f)),
                Height = 34f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(12f, 0f)
            };
            var dropdown = new Dropdown<TriStateFilter>
            {
                Height = 34f,
                Format = GlyphFormat.White.WithSize(.76f),
                MemberPadding = new Vector2(16f, 2f),
                LineHeight = 25f,
                DropdownHeight = 78f
            };
            dropdown.Add(new RichText("Either", GlyphFormat.White.WithSize(.76f)), TriStateFilter.Either);
            dropdown.Add(new RichText("Yes", GlyphFormat.White.WithSize(.76f)), TriStateFilter.Yes);
            dropdown.Add(new RichText("No", GlyphFormat.White.WithSize(.76f)), TriStateFilter.No);
            dropdown.ValueChanged += delegate
            {
                if (!updating && dropdown.Value != null)
                {
                    setValue(dropdown.Value.AssocMember);
                    RaiseChanged();
                }
            };

            var row = new HudChain(false)
            {
                Height = 34f,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 4f
            };
            row.Add(label, 1f);
            row.Add(dropdown, 1f);
            section.Add(row);
            return dropdown;
        }

        private NamedCheckBox AddGridSize(
            FilterSectionPanel section,
            string name,
            MyCubeSize size)
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
            section.Add(checkBox);
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
                TextPadding = new Vector2(16f, 0f),
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
                Padding = new Vector2(12f, 4f)
            };
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
