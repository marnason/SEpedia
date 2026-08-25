using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DefinitionView : HudElementBase
    {
        #region State and Construction

        public event Action<CatalogEntry> LinkClicked;

        private readonly DetailScrollBox content;
        private readonly List<HudElementBase> rows;
        private readonly List<DetailSectionGrid> sectionGrids;
        private readonly DefinitionHeader header;
        private readonly DetailPageComposer composer;
        private float lastRowWidth;
        private bool layoutDirty;

        public DefinitionView(
            DefinitionIndex index,
            CelestialIndex celestial,
            CatalogFilter filter,
            HudParentBase parent = null) : base(parent)
        {
            composer = new DetailPageComposer(index, celestial, filter);
            rows = new List<HudElementBase>();
            sectionGrids = new List<DetailSectionGrid>();
            lastRowWidth = -1f;
            layoutDirty = true;

            content = new DetailScrollBox(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Padding = new Vector2(8f),
                Spacing = 3f,
                UseSmoothScrolling = true
            };
            UiTheme.StyleVerticalScrollBar(content.ScrollBar);
            header = new DefinitionHeader();
            ShowMessage("Select a definition to inspect it.");
        }

        #endregion

        #region Navigation

        public void Show(DefinitionDocument definition)
        {
            Render(composer.Compose(new CatalogEntry(definition)));
        }

        public void Show(CatalogEntry entry)
        {
            if (entry == null)
            {
                ShowMessage("Select an entry to inspect it.");
                return;
            }
            Render(composer.Compose(entry));
        }

        public void ShowMessage(string message)
        {
            ClearRows();
            AddParagraph(message);
            ScrollToTop();
        }

        #endregion

        #region Layout

        protected override void Layout()
        {
            float rowWidth = GetAvailableRowWidth();
            if (!layoutDirty && Math.Abs(rowWidth - lastRowWidth) < .01f)
                return;

            ApplyRowWidth(rowWidth);
        }

        private float GetAvailableRowWidth()
        {
            return Math.Max(120f, content.UnpaddedSize.X - content.ScrollBar.Width);
        }

        private void ApplyRowWidth(float rowWidth)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                rows[index].Width = rowWidth;
                Label label = rows[index] as Label;
                if (label != null)
                    label.LineWrapWidth = Math.Max(80f, rowWidth - label.Padding.X);
            }
            header.SetWidth(rowWidth);
            for (int index = 0; index < sectionGrids.Count; index++)
                sectionGrids[index].SetWidth(rowWidth);

            lastRowWidth = rowWidth;
            layoutDirty = false;
        }

        #endregion

        #region Page Rendering

        private void Render(DetailPageModel page)
        {
            ClearRows();
            header.Update(page.Title, page.Id, page.RuntimeType, page.Description);
            AddRow(header.Root);

            DetailSectionGrid grid = null;
            for (int index = 0; index < page.Rows.Count; index++)
            {
                DetailRowModel row = page.Rows[index];
                switch (row.Kind)
                {
                    case DetailRowKind.Heading:
                        grid = null;
                        AddSection(row.Label);
                        break;
                    case DetailRowKind.Field:
                        grid = null;
                        AddKeyValue(row.Label, row.Value);
                        break;
                    case DetailRowKind.PagedSection:
                        if (grid == null)
                        {
                            grid = new DetailSectionGrid();
                            sectionGrids.Add(grid);
                            AddRow(grid.Root);
                        }
                        grid.Add(new PagedDetailSection(
                            RaiseLinkClicked,
                            row.Label,
                            row.Items,
                            row.Major,
                            row.HiddenItemCount));
                        break;
                }
            }

            float renderWidth = lastRowWidth > 0f
                ? lastRowWidth
                : GetAvailableRowWidth();
            ApplyRowWidth(renderWidth);
            ScrollToTop();
        }

        #endregion

        #region Row Construction

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
            sectionGrids.Clear();
            layoutDirty = true;
        }

        private void ScrollToTop()
        {
            content.ResetScroll();
        }

        #endregion

        #region Event Dispatch

        private void RaiseLinkClicked(CatalogEntry entry)
        {
            Action<CatalogEntry> handler = LinkClicked;
            if (handler != null)
                handler(entry);
        }

        #endregion
    }
}
