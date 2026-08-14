using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRage.Game;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DefinitionView : HudElementBase
    {
        public event Action<MyDefinitionId> LinkClicked;

        private readonly ScrollBox content;
        private readonly List<HudElementBase> rows;
        private readonly List<PagedDetailSection> pagedSections;
        private readonly DefinitionHeader header;
        private readonly DetailPageComposer composer;
        private float lastRowWidth;
        private bool layoutDirty;

        public DefinitionView(DefinitionIndex index, HudParentBase parent = null) : base(parent)
        {
            composer = new DetailPageComposer(index);
            rows = new List<HudElementBase>();
            pagedSections = new List<PagedDetailSection>();
            lastRowWidth = -1f;
            layoutDirty = true;

            content = new ScrollBox(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Padding = new Vector2(8f),
                Spacing = 3f,
                UseSmoothScrolling = true
            };
            header = new DefinitionHeader();
            ShowMessage("Select a definition to inspect it.");
        }

        public void Show(DefinitionDocument definition)
        {
            Render(composer.Compose(definition));
        }

        public void Show(CatalogEntry entry)
        {
            if (entry == null)
            {
                ShowMessage("Select an entry to inspect it.");
                return;
            }
            Render(entry.Definition != null
                ? composer.Compose(entry.Definition)
                : composer.Compose(entry.Planet));
        }

        public void ShowMessage(string message)
        {
            ClearRows();
            AddParagraph(message);
        }

        protected override void Layout()
        {
            float rowWidth = Math.Max(120f, UnpaddedSize.X - content.ScrollBar.Width - content.Padding.X - 12f);
            if (!layoutDirty && Math.Abs(rowWidth - lastRowWidth) < .01f)
                return;

            for (int index = 0; index < rows.Count; index++)
            {
                rows[index].Width = rowWidth;
                Label label = rows[index] as Label;
                if (label != null)
                    label.LineWrapWidth = Math.Max(80f, rowWidth - label.Padding.X);
            }
            header.SetWidth(rowWidth);
            for (int index = 0; index < pagedSections.Count; index++)
                pagedSections[index].SetWidth(rowWidth);

            lastRowWidth = rowWidth;
            layoutDirty = false;
        }

        private void Render(DetailPageModel page)
        {
            ClearRows();
            header.Update(page.Title, page.Id, page.RuntimeType, page.Description, page.Icon);
            AddRow(header.Root);

            for (int index = 0; index < page.Rows.Count; index++)
            {
                DetailRowModel row = page.Rows[index];
                switch (row.Kind)
                {
                    case DetailRowKind.Heading:
                        AddSection(row.Label);
                        break;
                    case DetailRowKind.Field:
                        AddKeyValue(row.Label, row.Value);
                        break;
                    case DetailRowKind.Paragraph:
                        AddParagraph(row.Value);
                        break;
                    case DetailRowKind.Link:
                        AddDefinitionLink(row.Link);
                        break;
                    case DetailRowKind.PagedSection:
                        AddPagedSection(row.Label, row.Items, row.Major);
                        break;
                }
            }
            content.Start = 0;
        }

        private void AddDefinitionLink(DetailItem item)
        {
            if (item == null || !item.LinkId.HasValue)
            {
                AddParagraph(item != null ? item.Text : string.Empty);
                return;
            }

            MyDefinitionId id = item.LinkId.Value;
            var link = new LabelButton
            {
                Text = new RichText(item.Text, GlyphFormat.Blueish.WithStyle(FontStyles.Underline).WithSize(.88f)),
                BuilderMode = TextBuilderModes.Wrapped,
                AutoResize = true,
                VertCenterText = false,
                Padding = new Vector2(12f, 2f)
            };
            link.MouseInput.LeftClicked += delegate { RaiseLinkClicked(id); };
            AddRow(link);
        }

        private void AddPagedSection(string heading, IReadOnlyList<DetailItem> items, bool major)
        {
            var mutableItems = new List<DetailItem>(items);
            var section = new PagedDetailSection(RaiseLinkClicked, heading, mutableItems, major);
            pagedSections.Add(section);
            AddRow(section.Root);
        }

        private void AddSection(string text)
        {
            AddLabel(new RichText(text, GlyphFormat.Blueish.WithSize(1.02f)), 31f, new Vector2(8f, 7f));
        }

        private void AddKeyValue(string key, string value)
        {
            var text = new RichText();
            text.Add(key + ": ", GlyphFormat.Blueish.WithSize(.82f));
            text.Add(value ?? string.Empty, GlyphFormat.White.WithSize(.82f));
            AddLabel(text, 24f, new Vector2(8f, 2f));
        }

        private void AddParagraph(string text)
        {
            var label = new Label
            {
                Text = new RichText(text ?? string.Empty, GlyphFormat.White.WithSize(.82f)),
                BuilderMode = TextBuilderModes.Wrapped,
                AutoResize = true,
                VertCenterText = false,
                Padding = new Vector2(8f, 4f)
            };
            AddRow(label);
        }

        private void AddLabel(RichText text, float height, Vector2 padding)
        {
            var label = new Label
            {
                Text = text,
                Height = height,
                AutoResize = false,
                VertCenterText = true,
                Padding = padding
            };
            AddRow(label);
        }

        private void AddRow(HudElementBase row)
        {
            rows.Add(row);
            content.Add(row);
            layoutDirty = true;
        }

        private void ClearRows()
        {
            content.Clear();
            rows.Clear();
            pagedSections.Clear();
            layoutDirty = true;
        }

        private void RaiseLinkClicked(MyDefinitionId id)
        {
            Action<MyDefinitionId> handler = LinkClicked;
            if (handler != null)
                handler(id);
        }
    }
}
