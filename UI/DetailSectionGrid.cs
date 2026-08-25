using System;
using System.Collections.Generic;
using RichHudFramework.UI;

namespace SEpedia.UI
{
    internal sealed class DetailSectionGrid
    {
        private readonly List<PagedDetailSection> sections;
        private readonly List<HudChain> rows;
        private float lastWidth;
        private int lastColumnCount;

        public readonly HudChain Root;

        public DetailSectionGrid()
        {
            sections = new List<PagedDetailSection>();
            rows = new List<HudChain>();
            lastWidth = -1f;
            lastColumnCount = -1;
            Root = new HudChain(true)
            {
                SizingMode = HudChainSizingModes.FitChainAlignAxis |
                    HudChainSizingModes.FitMembersOffAxis |
                    HudChainSizingModes.AlignMembersStart,
                Spacing = UiTheme.DetailGridSpacing
            };
        }

        #region Content

        public void Add(PagedDetailSection section)
        {
            sections.Add(section);
            lastColumnCount = -1;
        }

        #endregion

        #region Responsive Layout

        public void SetWidth(float width)
        {
            width = Math.Max(120f, width);
            int availableColumns = Math.Max(1, (int)(width / UiTheme.DetailGridMinimumCellWidth));
            int columns = Math.Max(1, Math.Min(sections.Count, availableColumns));
            if (columns == lastColumnCount && Math.Abs(width - lastWidth) < .01f)
                return;

            float cellWidth = (width - UiTheme.DetailGridSpacing * (columns - 1)) / columns;
            int requiredRows = (sections.Count + columns - 1) / columns;
            EnsureRows(requiredRows);
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                rows[rowIndex].Clear();
            Root.Clear();

            int sectionIndex = 0;
            for (int rowIndex = 0; rowIndex < requiredRows; rowIndex++)
            {
                HudChain row = rows[rowIndex];
                row.Width = width;
                for (int column = 0; column < columns && sectionIndex < sections.Count; column++)
                {
                    PagedDetailSection section = sections[sectionIndex++];
                    section.SetWidth(cellWidth);
                    row.Add(section.Root);
                }
                Root.Add(row);
            }

            Root.Width = width;
            lastWidth = width;
            lastColumnCount = columns;
        }

        private void EnsureRows(int count)
        {
            while (rows.Count < count)
            {
                rows.Add(new HudChain(false)
                {
                    SizingMode = HudChainSizingModes.FitChainOffAxis |
                        HudChainSizingModes.AlignMembersStart,
                    Spacing = UiTheme.DetailGridSpacing
                });
            }
        }

        #endregion
    }
}
