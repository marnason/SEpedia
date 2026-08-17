using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;

namespace SEpedia.UI
{
    internal sealed class PagedFacetSection
    {
        #region State

        private sealed class Slot
        {
            public readonly NamedCheckBox CheckBox;
            public FacetCount Facet;

            public Slot(NamedCheckBox checkBox)
            {
                CheckBox = checkBox;
            }
        }

        private static readonly IReadOnlyList<FacetCount> EmptyFacets =
            new List<FacetCount>().AsReadOnly();

        private readonly string headingText;
        private readonly HashSet<string> selected;
        private readonly bool showKeyToolTips;
        private readonly Action changed;
        private readonly FilterSectionPanel section;
        private readonly Label heading;
        private readonly NamedCheckBox all;
        private readonly Slot[] slots;
        private readonly PagerRow pager;
        private IReadOnlyList<FacetCount> facets;

        #endregion

        #region Construction

        public PagedFacetSection(
            ScrollBox content,
            string headingText,
            string allText,
            HashSet<string> selected,
            bool showKeyToolTips,
            Action changed,
            IList<ScrollBoxEntry> group = null)
        {
            this.headingText = headingText;
            this.selected = selected;
            this.showKeyToolTips = showKeyToolTips;
            this.changed = changed;
            facets = EmptyFacets;
            section = new FilterSectionPanel(content, group);

            heading = CreateHeading(headingText);
            section.Add(heading);

            all = CreateCheckBox(allText, true);
            all.MouseInput.LeftClicked += delegate
            {
                if (all.Value)
                {
                    selected.Clear();
                    changed();
                }
                else
                {
                    all.Value = true;
                }
            };
            section.Add(all);

            slots = new Slot[UiTheme.AdvancedFilterPageSize];
            for (int index = 0; index < slots.Length; index++)
            {
                NamedCheckBox checkBox = CreateCheckBox(string.Empty, false);
                var slot = new Slot(checkBox);
                section.Add(checkBox);
                slots[index] = slot;
                Slot capturedSlot = slot;
                checkBox.MouseInput.LeftClicked += delegate
                {
                    if (capturedSlot.Facet == null)
                        return;
                    if (capturedSlot.CheckBox.Value)
                        selected.Add(capturedSlot.Facet.Key);
                    else
                        selected.Remove(capturedSlot.Facet.Key);
                    changed();
                };
            }

            pager = new PagerRow(UpdateVisibleSlots);
            section.Add(pager.Root);
        }

        #endregion

        #region Facet Updates

        public void Update(IReadOnlyList<FacetCount> newFacets, bool enabled)
        {
            facets = newFacets ?? EmptyFacets;
            pager.Configure(facets.Count, UiTheme.AdvancedFilterPageSize);
            SetEnabled(enabled);
            UpdateVisibleSlots();
        }

        public void SetEnabled(bool value)
        {
            section.Entry.Enabled = value;
        }

        private void UpdateVisibleSlots()
        {
            int start = pager.Page * UiTheme.AdvancedFilterPageSize;
            heading.Text = new RichText(
                selected.Count == 0 ? headingText : headingText + " (" + selected.Count + " selected)",
                GlyphFormat.Blueish.WithSize(.88f));
            all.Value = selected.Count == 0;

            for (int index = 0; index < slots.Length; index++)
            {
                Slot slot = slots[index];
                int facetIndex = start + index;
                slot.Facet = facetIndex < facets.Count ? facets[facetIndex] : null;
                slot.CheckBox.Visible = slot.Facet != null;
                if (slot.Facet == null)
                {
                    slot.CheckBox.Name = new RichText(string.Empty, GlyphFormat.White.WithSize(.72f));
                    slot.CheckBox.MouseInput.ToolTip = null;
                    slot.CheckBox.Value = false;
                }
                else
                {
                    slot.CheckBox.Name = new RichText(
                        slot.Facet.DisplayName + " (" + slot.Facet.Count + ")",
                        GlyphFormat.White.WithSize(.72f));
                    slot.CheckBox.Value = selected.Contains(slot.Facet.Key);
                    slot.CheckBox.MouseInput.ToolTip = showKeyToolTips ? slot.Facet.Key : null;
                }
            }
        }

        #endregion

        #region Control Factories

        private static NamedCheckBox CreateCheckBox(string name, bool value)
        {
            return new NamedCheckBox
            {
                Name = new RichText(name, GlyphFormat.White.WithSize(.72f)),
                Height = UiTheme.StandardRowHeight,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = new VRageMath.Vector2(16f, 0f),
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
                Padding = new VRageMath.Vector2(12f, 4f)
            };
        }

        #endregion
    }
}
