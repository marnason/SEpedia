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

        private readonly CatalogFilter filter;
        private readonly ScrollBox content;
        private readonly List<HudElementBase> rows;
        private bool updating;

        public AdvancedFilterDrawer(CatalogFilter filter, HudParentBase parent = null) : base(parent)
        {
            this.filter = filter;
            rows = new List<HudElementBase>();
            Width = 300f;

            content = new ScrollBox(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Padding = new Vector2(7f),
                Spacing = 3f,
                UseSmoothScrolling = true
            };
        }

        public void Refresh(CatalogResult result)
        {
            updating = true;
            try
            {
                content.Clear();
                rows.Clear();

                var reset = new LabelBoxButton
                {
                    Text = new RichText("X", GlyphFormat.White.WithSize(.9f)),
                    Height = 27f,
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
                AddRow(reset);

                AddHeading("Definition flags");
                AddTriState("Enabled", filter.Enabled, delegate(TriStateFilter value) { filter.Enabled = value; });
                AddTriState("Public", filter.Public, delegate(TriStateFilter value) { filter.Public = value; });
                AddTriState("Survival", filter.AvailableInSurvival, delegate(TriStateFilter value) { filter.AvailableInSurvival = value; });

                if (filter.Category == BrowseCategory.Blocks)
                {
                    AddTriState("Listed in G menu", filter.ListedInBuildMenu, delegate(TriStateFilter value) { filter.ListedInBuildMenu = value; });
                    AddHeading("Grid size");
                    AddGridSize("Small", MyCubeSize.Small);
                    AddGridSize("Large", MyCubeSize.Large);

                    AddHeading("Runtime block type");
                    AddAllCheckBox("All block types", filter.SelectedBlockTypes.Count == 0, delegate
                    {
                        filter.SelectedBlockTypes.Clear();
                    });
                    for (int index = 0; index < result.BlockTypes.Count; index++)
                        AddBlockType(result.BlockTypes[index]);
                }

                AddHeading("Source");
                AddAllCheckBox("All sources", filter.SelectedSourceKeys.Count == 0, delegate
                {
                    filter.SelectedSourceKeys.Clear();
                });
                for (int index = 0; index < result.Sources.Count; index++)
                    AddSource(result.Sources[index]);
            }
            finally
            {
                updating = false;
            }
        }

        protected override void Layout()
        {
            float width = Math.Max(100f, UnpaddedSize.X - content.ScrollBar.Width - content.Padding.X - 10f);
            for (int index = 0; index < rows.Count; index++)
                rows[index].Width = width;
        }

        private void AddTriState(string name, TriStateFilter current, Action<TriStateFilter> setValue)
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
            dropdown.SetSelectionAt((int)current);
            dropdown.ValueChanged += delegate
            {
                if (!updating && dropdown.Value != null)
                {
                    setValue(dropdown.Value.AssocMember);
                    RaiseChanged();
                }
            };
            AddRow(dropdown);
        }

        private void AddGridSize(string name, MyCubeSize size)
        {
            NamedCheckBox checkBox = CreateCheckBox(name, filter.SelectedGridSizes.Contains(size));
            checkBox.ValueChanged += delegate
            {
                if (updating)
                    return;
                if (checkBox.Value)
                    filter.SelectedGridSizes.Add(size);
                else if (filter.SelectedGridSizes.Count > 1)
                    filter.SelectedGridSizes.Remove(size);
                else
                {
                    updating = true;
                    checkBox.Value = true;
                    updating = false;
                    return;
                }
                RaiseChanged();
            };
            AddRow(checkBox);
        }

        private void AddSource(FacetCount facet)
        {
            NamedCheckBox checkBox = CreateCheckBox(facet.DisplayName + " (" + facet.Count + ")", filter.SelectedSourceKeys.Contains(facet.Key));
            checkBox.ValueChanged += delegate
            {
                if (updating)
                    return;
                if (checkBox.Value)
                    filter.SelectedSourceKeys.Add(facet.Key);
                else
                    filter.SelectedSourceKeys.Remove(facet.Key);
                RaiseChanged();
            };
            AddRow(checkBox);
        }

        private void AddBlockType(FacetCount facet)
        {
            NamedCheckBox checkBox = CreateCheckBox(facet.DisplayName + " (" + facet.Count + ")", filter.SelectedBlockTypes.Contains(facet.Key));
            checkBox.MouseInput.ToolTip = facet.Key;
            checkBox.ValueChanged += delegate
            {
                if (updating)
                    return;
                if (checkBox.Value)
                    filter.SelectedBlockTypes.Add(facet.Key);
                else
                    filter.SelectedBlockTypes.Remove(facet.Key);
                RaiseChanged();
            };
            AddRow(checkBox);
        }

        private void AddAllCheckBox(string name, bool value, Action selectAll)
        {
            NamedCheckBox checkBox = CreateCheckBox(name, value);
            checkBox.ValueChanged += delegate
            {
                if (updating)
                    return;

                if (checkBox.Value)
                {
                    selectAll();
                    RaiseChanged();
                }
                else
                {
                    updating = true;
                    checkBox.Value = true;
                    updating = false;
                }
            };
            AddRow(checkBox);
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

        private void AddHeading(string text)
        {
            var label = new Label
            {
                Text = new RichText(text, GlyphFormat.Blueish.WithSize(.88f)),
                Height = 28f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(4f, 4f)
            };
            AddRow(label);
        }

        private void AddRow(HudElementBase row)
        {
            rows.Add(row);
            content.Add(row);
        }

        private void RaiseChanged()
        {
            Action handler = FiltersChanged;
            if (handler != null)
                handler();
        }
    }
}
