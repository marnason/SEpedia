using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class CategoryBar
    {
        #region State

        private sealed class CategoryButton
        {
            public readonly CatalogCategoryDefinition Category;
            public readonly LabelBoxButton Button;
            public readonly float MinimumWidth;
            public bool Available;

            public CategoryButton(
                CatalogCategoryDefinition category,
                LabelBoxButton button,
                float minimumWidth)
            {
                Category = category;
                Button = button;
                MinimumWidth = minimumWidth;
                Available = true;
            }
        }

        private const float RowSpacing = 2f;
        private readonly CatalogFilter filter;
        private readonly Action categoryChanged;
        private readonly List<CategoryButton> buttons;
        private readonly List<HudChain> rows;
        private int activeRows;
        private float availableWidth;

        public HudChain Root { get; private set; }

        #endregion

        #region Construction

        public CategoryBar(CatalogFilter filter, Action categoryChanged)
        {
            this.filter = filter;
            this.categoryChanged = categoryChanged;
            buttons = new List<CategoryButton>();
            rows = new List<HudChain>();
            availableWidth = 1f;

            Root = new HudChain(true)
            {
                SizingMode = HudChainSizingModes.FitChainAlignAxis | HudChainSizingModes.FitMembersOffAxis,
                Spacing = RowSpacing
            };

            for (int index = 0; index < filter.Schema.Categories.Count; index++)
                Add(filter.Schema.Categories[index]);
            Reflow(1);
            UpdateSelection();
        }

        #endregion

        #region Responsive Layout

        public void UpdateLayout(float availableWidth)
        {
            this.availableWidth = Math.Max(1f, availableWidth);
            List<CategoryButton> availableButtons = GetAvailableButtons();
            int rowCount = GetRowCount(availableButtons, this.availableWidth);
            if (rowCount != activeRows)
                Reflow(availableButtons, rowCount);
        }

        public void UpdateAvailability(Func<CatalogCategoryDefinition, bool> isAvailable)
        {
            CategoryButton firstAvailable = null;
            bool selectedAvailable = false;

            for (int index = 0; index < buttons.Count; index++)
            {
                CategoryButton categoryButton = buttons[index];
                categoryButton.Available = isAvailable != null && isAvailable(categoryButton.Category);
                if (!categoryButton.Available)
                    continue;
                if (firstAvailable == null)
                    firstAvailable = categoryButton;
                selectedAvailable |= categoryButton.Category.Key == filter.CategoryKey;
            }

            if (!selectedAvailable && firstAvailable != null)
                filter.CategoryKey = firstAvailable.Category.Key;

            List<CategoryButton> availableButtons = GetAvailableButtons();
            Reflow(availableButtons, GetRowCount(availableButtons, availableWidth));
            UpdateSelection();
        }

        private List<CategoryButton> GetAvailableButtons()
        {
            var availableButtons = new List<CategoryButton>();
            for (int index = 0; index < buttons.Count; index++)
            {
                if (buttons[index].Available)
                    availableButtons.Add(buttons[index]);
            }
            return availableButtons;
        }

        private static int GetRowCount(IReadOnlyList<CategoryButton> availableButtons, float width)
        {
            if (availableButtons.Count == 0)
                return 1;

            int rowCount = 1;
            float rowWidth = 0f;
            for (int index = 0; index < availableButtons.Count; index++)
            {
                float buttonWidth = availableButtons[index].MinimumWidth;
                float candidateWidth = rowWidth > 0f
                    ? rowWidth + RowSpacing + buttonWidth
                    : buttonWidth;
                if (rowWidth > 0f && candidateWidth > width)
                {
                    rowCount++;
                    rowWidth = buttonWidth;
                }
                else
                    rowWidth = candidateWidth;
            }
            return rowCount;
        }

        #endregion

        #region Selection and Buttons

        public void UpdateSelection()
        {
            for (int index = 0; index < buttons.Count; index++)
            {
                CategoryButton categoryButton = buttons[index];
                bool selected = categoryButton.Category.Key == filter.CategoryKey;
                categoryButton.Button.Color = selected ? UiTheme.Selected : UiTheme.Panel;
                categoryButton.Button.Format = selected
                    ? GlyphFormat.White.WithColor(UiTheme.SelectedText).WithAlignment(TextAlignment.Center).WithSize(.68f)
                    : GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.68f);
                categoryButton.Button.HighlightEnabled = !selected;
            }
        }

        private void Add(CatalogCategoryDefinition category)
        {
            string name = category.DisplayName;
            var button = new LabelBoxButton
            {
                Text = new RichText(name, GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.68f)),
                Height = 29f,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = new Vector2(4f, 0f),
                Color = UiTheme.Panel,
                HighlightColor = UiTheme.PanelHighlight
            };
            button.MouseInput.LeftClicked += delegate
            {
                if (filter.CategoryKey == category.Key)
                    return;
                filter.CategoryKey = category.Key;
                UpdateSelection();
                if (categoryChanged != null)
                    categoryChanged();
            };
            buttons.Add(new CategoryButton(category, button, Math.Max(62f, name.Length * 8f + 24f)));
        }

        #endregion

        #region Row Distribution

        private void Reflow(int rowCount)
        {
            List<CategoryButton> availableButtons = GetAvailableButtons();
            Reflow(availableButtons, rowCount);
        }

        private void Reflow(IReadOnlyList<CategoryButton> availableButtons, int rowCount)
        {
            EnsureRows(rowCount);
            for (int row = 0; row < rows.Count; row++)
                rows[row].Clear();

            int[] rowEnds = GetBalancedRowEnds(availableButtons, rowCount);
            int start = 0;
            for (int row = 0; row < rowCount; row++)
            {
                int end = rowEnds[row];
                for (int index = start; index < end; index++)
                {
                    CategoryButton button = availableButtons[index];
                    rows[row].Add(button.Button, button.MinimumWidth);
                }
                start = end;
            }

            activeRows = rowCount;
            for (int row = 0; row < rows.Count; row++)
                rows[row].Visible = row < rowCount;
        }

        private void EnsureRows(int rowCount)
        {
            while (rows.Count < rowCount)
            {
                var row = new HudChain(false)
                {
                    Height = 29f,
                    SizingMode = HudChainSizingModes.FitMembersOffAxis,
                    Spacing = RowSpacing
                };
                rows.Add(row);
                Root.Add(row);
            }
        }

        private static int[] GetBalancedRowEnds(
            IReadOnlyList<CategoryButton> availableButtons,
            int rowCount)
        {
            int buttonCount = availableButtons.Count;
            var rowEnds = new int[rowCount];
            if (buttonCount == 0)
                return rowEnds;

            var prefixWidths = new float[buttonCount + 1];
            for (int index = 0; index < buttonCount; index++)
                prefixWidths[index + 1] = prefixWidths[index] + availableButtons[index].MinimumWidth;

            var costs = new float[rowCount + 1, buttonCount + 1];
            var balanceCosts = new float[rowCount + 1, buttonCount + 1];
            var splits = new int[rowCount + 1, buttonCount + 1];
            for (int row = 0; row <= rowCount; row++)
            {
                for (int end = 0; end <= buttonCount; end++)
                {
                    costs[row, end] = float.PositiveInfinity;
                    balanceCosts[row, end] = float.PositiveInfinity;
                }
            }
            costs[0, 0] = 0f;
            balanceCosts[0, 0] = 0f;

            for (int row = 1; row <= rowCount; row++)
            {
                for (int end = row; end <= buttonCount; end++)
                {
                    for (int split = row - 1; split < end; split++)
                    {
                        int buttonsInRow = end - split;
                        float currentWidth = prefixWidths[end] - prefixWidths[split] +
                            Math.Max(0, buttonsInRow - 1) * RowSpacing;
                        float candidate = Math.Max(costs[row - 1, split], currentWidth);
                        float balanceCandidate = balanceCosts[row - 1, split] + currentWidth * currentWidth;
                        if (candidate < costs[row, end] ||
                            (Math.Abs(candidate - costs[row, end]) < .01f &&
                                balanceCandidate < balanceCosts[row, end]))
                        {
                            costs[row, end] = candidate;
                            balanceCosts[row, end] = balanceCandidate;
                            splits[row, end] = split;
                        }
                    }
                }
            }

            int rowEnd = buttonCount;
            for (int row = rowCount; row > 0; row--)
            {
                rowEnds[row - 1] = rowEnd;
                rowEnd = splits[row, rowEnd];
            }
            return rowEnds;
        }

        #endregion
    }
}
