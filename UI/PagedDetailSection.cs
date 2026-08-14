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
        private const int PageSize = 8;
        private static readonly IReadOnlyList<DetailItem> EmptyItems = new List<DetailItem>().AsReadOnly();

        private readonly Action<MyDefinitionId> linkClicked;
        private readonly Label heading;
        private readonly LabelButton[] slots;
        private readonly LabelBoxButton previous;
        private readonly Label pageLabel;
        private readonly LabelBoxButton next;
        private readonly HudChain pager;
        private readonly IReadOnlyList<DetailItem> items;
        private int page;

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

            slots = new LabelButton[PageSize];
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

            previous = CreatePagerButton("<", "Previous page");
            pageLabel = new Label
            {
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
                    UpdateSlots();
                }
            };
            next.MouseInput.LeftClicked += delegate
            {
                if (page < PageCount - 1)
                {
                    page++;
                    UpdateSlots();
                }
            };

            pager = new HudChain(false)
            {
                Height = 27f,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 4f
            };
            pager.Add(previous);
            pager.Add(pageLabel, 1f);
            pager.Add(next);
            Root.Add(pager);
            UpdateSlots();
        }

        private int PageCount
        {
            get { return Math.Max(1, (items.Count + PageSize - 1) / PageSize); }
        }

        public void SetWidth(float width)
        {
            Root.Width = width;
            heading.Width = width;
            pager.Width = width;
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index].Width = width;
                slots[index].LineWrapWidth = Math.Max(60f, width - slots[index].Padding.X);
            }
        }

        private void Activate(int slotIndex)
        {
            int itemIndex = page * PageSize + slotIndex;
            if (itemIndex >= items.Count || !items[itemIndex].LinkId.HasValue || linkClicked == null)
                return;
            linkClicked(items[itemIndex].LinkId.Value);
        }

        private void UpdateSlots()
        {
            page = Math.Max(0, Math.Min(page, PageCount - 1));
            int start = page * PageSize;
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

            pager.Visible = PageCount > 1;
            pageLabel.Text = new RichText(
                (page + 1) + " / " + PageCount,
                GlyphFormat.Blueish.WithAlignment(TextAlignment.Center).WithSize(.72f));
            previous.InputEnabled = page > 0;
            next.InputEnabled = page < PageCount - 1;
            previous.Color = previous.InputEnabled ? new Color(36, 47, 55) : new Color(28, 35, 40);
            next.Color = next.InputEnabled ? new Color(36, 47, 55) : new Color(28, 35, 40);
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
}
