using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRage.Game;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DetailItem
    {
        public readonly string Text;
        public readonly MyDefinitionId? LinkId;

        public DetailItem(string text, MyDefinitionId? linkId = null)
        {
            Text = text ?? string.Empty;
            LinkId = linkId;
        }
    }

    internal sealed class PagedDetailSection
    {
        private static readonly IReadOnlyList<DetailItem> EmptyItems = new List<DetailItem>().AsReadOnly();

        private readonly Action<MyDefinitionId> linkClicked;
        private readonly Label heading;
        private readonly LabelButton[] slots;
        private readonly PagerRow pager;
        private readonly IReadOnlyList<DetailItem> items;

        public readonly HudChain Root;

        public PagedDetailSection(
            Action<MyDefinitionId> linkClicked,
            string headingText,
            IList<DetailItem> sectionItems,
            bool majorHeading)
        {
            this.linkClicked = linkClicked;
            items = sectionItems != null
                ? new List<DetailItem>(sectionItems).AsReadOnly()
                : EmptyItems;

            heading = new Label
            {
                Text = new RichText(
                    headingText,
                    majorHeading ? GlyphFormat.Blueish.WithSize(1.02f) : GlyphFormat.White.WithSize(.92f)),
                Height = majorHeading ? 31f : 25f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(8f, majorHeading ? 7f : 3f)
            };

            Root = new HudChain(true)
            {
                SizingMode = HudChainSizingModes.FitChainAlignAxis | HudChainSizingModes.FitMembersOffAxis,
                Spacing = 1f
            };
            Root.Add(heading);

            slots = new LabelButton[UiTheme.BoundedPageSize];
            for (int index = 0; index < slots.Length; index++)
            {
                var slot = new LabelButton
                {
                    BuilderMode = TextBuilderModes.Wrapped,
                    AutoResize = true,
                    VertCenterText = false,
                    Padding = new Vector2(12f, 2f),
                    Visible = false
                };
                int capturedIndex = index;
                slot.MouseInput.LeftClicked += delegate { Activate(capturedIndex); };
                slots[index] = slot;
                Root.Add(slot);
            }

            pager = new PagerRow(UpdateSlots);
            pager.Configure(items.Count, UiTheme.BoundedPageSize);
            Root.Add(pager.Root);
            UpdateSlots();
        }

        public void SetWidth(float width)
        {
            Root.Width = width;
            heading.Width = width;
            pager.Root.Width = width;
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Width = width;
                slots[index].LineWrapWidth = Math.Max(60f, width - slots[index].Padding.X);
            }
        }

        private void Activate(int slotIndex)
        {
            int itemIndex = pager.Page * UiTheme.BoundedPageSize + slotIndex;
            if (itemIndex >= items.Count || !items[itemIndex].LinkId.HasValue || linkClicked == null)
                return;
            linkClicked(items[itemIndex].LinkId.Value);
        }

        private void UpdateSlots()
        {
            int start = pager.Page * UiTheme.BoundedPageSize;
            for (int index = 0; index < slots.Length; index++)
            {
                int itemIndex = start + index;
                LabelButton slot = slots[index];
                bool visible = itemIndex < items.Count;
                slot.Visible = visible;
                if (!visible)
                {
                    slot.InputEnabled = false;
                    continue;
                }

                DetailItem item = items[itemIndex];
                bool linked = item.LinkId.HasValue;
                slot.Text = new RichText(
                    item.Text,
                    linked
                        ? GlyphFormat.Blueish.WithStyle(FontStyles.Underline).WithSize(.88f)
                        : GlyphFormat.White.WithSize(.82f));
                slot.InputEnabled = linked;
            }

        }
    }
}
