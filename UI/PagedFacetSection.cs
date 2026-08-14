using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;

namespace SEpedia.UI
{
    internal sealed class PagedFacetSection
    {
        private sealed class Slot
        {
            public readonly NamedCheckBox CheckBox;
            public readonly ScrollBoxEntry Entry;
            public FacetCount Facet;

            public Slot(NamedCheckBox checkBox, ScrollBoxEntry entry)
            {
                CheckBox = checkBox;
                Entry = entry;
            }
        }

        private static readonly IReadOnlyList<FacetCount> EmptyFacets =
            new List<FacetCount>().AsReadOnly();

        private readonly string headingText;
        private readonly HashSet<string> selected;
        private readonly bool showKeyToolTips;
        private readonly Func<bool> isUpdating;
        private readonly Action changed;
        private readonly Label heading;
        private readonly NamedCheckBox all;
        private readonly ScrollBoxEntry headingEntry;
        private readonly ScrollBoxEntry allEntry;
        private readonly Slot[] slots;
        private readonly PagerRow pager;
        private readonly ScrollBoxEntry pagerEntry;
        private IReadOnlyList<FacetCount> facets;

        public PagedFacetSection(
            ScrollBox content,
            string headingText,
            string allText,
            HashSet<string> selected,
            bool showKeyToolTips,
            Func<bool> isUpdating,
            Action changed,
            IList<ScrollBoxEntry> group = null)
        {
            this.headingText = headingText;
            this.selected = selected;
            this.showKeyToolTips = showKeyToolTips;
            this.isUpdating = isUpdating;
            this.changed = changed;
            facets = EmptyFacets;

            heading = CreateHeading(headingText);
            headingEntry = AddRow(content, heading, group);

            all = CreateCheckBox(allText, true);
            all.MouseInput.LeftClicked += delegate
            {
                if (isUpdating())
                    return;
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
            allEntry = AddRow(content, all, group);

            slots = new Slot[UiTheme.BoundedPageSize];
            for (int index = 0; index < slots.Length; index++)
            {
                NamedCheckBox checkBox = CreateCheckBox(string.Empty, false);
                var slot = new Slot(checkBox, AddRow(content, checkBox, group));
                slots[index] = slot;
                Slot capturedSlot = slot;
                checkBox.MouseInput.LeftClicked += delegate
                {
                    if (isUpdating() || capturedSlot.Facet == null)
                        return;
                    if (capturedSlot.CheckBox.Value)
                        selected.Add(capturedSlot.Facet.Key);
                    else
                        selected.Remove(capturedSlot.Facet.Key);
                    changed();
                };
            }

            pager = new PagerRow(UpdateVisibleSlots);
            pagerEntry = AddRow(content, pager.Root, group);
        }

        public void Update(IReadOnlyList<FacetCount> newFacets, bool enabled)
        {
            facets = newFacets ?? EmptyFacets;
            pager.Configure(facets.Count, UiTheme.BoundedPageSize);
            SetEnabled(enabled);
            UpdateVisibleSlots();
        }

        public void SetEnabled(bool enabled)
        {
            headingEntry.Enabled = enabled;
            allEntry.Enabled = enabled;
            for (int index = 0; index < slots.Length; index++)
                slots[index].Entry.Enabled = enabled && slots[index].Facet != null;
            pagerEntry.Enabled = enabled && pager.Root.Visible;
        }

        private void UpdateVisibleSlots()
        {
            bool enabled = headingEntry.Enabled;
            int start = pager.Page * UiTheme.BoundedPageSize;
            heading.Text = new RichText(
                selected.Count == 0 ? headingText : headingText + " (" + selected.Count + " selected)",
                GlyphFormat.Blueish.WithSize(.88f));
            all.Value = selected.Count == 0;

            for (int index = 0; index < slots.Length; index++)
            {
                Slot slot = slots[index];
                int facetIndex = start + index;
                slot.Facet = facetIndex < facets.Count ? facets[facetIndex] : null;
                slot.Entry.Enabled = enabled && slot.Facet != null;
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
            pagerEntry.Enabled = enabled && pager.Root.Visible;
        }

        private static ScrollBoxEntry AddRow(
            ScrollBox content,
            HudElementBase row,
            IList<ScrollBoxEntry> group)
        {
            var entry = new ScrollBoxEntry();
            entry.SetElement(row);
            content.Add(entry);
            if (group != null)
                group.Add(entry);
            return entry;
        }

        private static NamedCheckBox CreateCheckBox(string name, bool value)
        {
            return new NamedCheckBox
            {
                Name = new RichText(name, GlyphFormat.White.WithSize(.72f)),
                Height = UiTheme.StandardRowHeight,
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
                Padding = new VRageMath.Vector2(4f, 4f)
            };
        }
    }
}
