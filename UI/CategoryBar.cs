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
            public readonly BrowseCategory Category;
            public readonly LabelBoxButton Button;
            public readonly float MinimumWidth;
            public bool Available;

            public CategoryButton(
                BrowseCategory category,
                LabelBoxButton button,
                float minimumWidth)
            {
                Category = category;
                Button = button;
                MinimumWidth = minimumWidth;
                Available = true;
            }
        }

        private const int MaximumRows = 3;
        private readonly CatalogFilter filter;
        private readonly Action categoryChanged;
        private readonly List<CategoryButton> buttons;
        private readonly HudChain[] rows;
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
            rows = new HudChain[MaximumRows];
            availableWidth = 1f;

            Root = new HudChain(true)
            {
                SizingMode = HudChainSizingModes.FitChainAlignAxis | HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f
            };
            for (int index = 0; index < rows.Length; index++)
            {
                rows[index] = new HudChain(false)
                {
                    Height = 29f,
                    SizingMode = HudChainSizingModes.FitMembersOffAxis,
                    Spacing = 2f
                };
                Root.Add(rows[index]);
            }

            Add(BrowseCategory.Components);
            Add(BrowseCategory.Ores);
            Add(BrowseCategory.Ingots);
            Add(BrowseCategory.Ammo);
            Add(BrowseCategory.ToolsAndWeapons);
            Add(BrowseCategory.Consumables);
            Add(BrowseCategory.Items);
            Add(BrowseCategory.Blocks);
            Add(BrowseCategory.Recipes);
            Add(BrowseCategory.Celestial);
            Reflow(1);
            UpdateSelection();
        }

        #endregion

        #region Responsive Layout

        public void UpdateLayout(float availableWidth)
        {
            this.availableWidth = Math.Max(1f, availableWidth);
            int rowCount = GetRowCount(this.availableWidth);
            if (rowCount != activeRows)
                Reflow(rowCount);
        }

        public void UpdateAvailability(Func<BrowseCategory, bool> isAvailable)
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
                selectedAvailable |= categoryButton.Category == filter.Category;
            }

            if (!selectedAvailable && firstAvailable != null)
                filter.Category = firstAvailable.Category;

            Reflow(GetRowCount(availableWidth));
            UpdateSelection();
        }

        private int GetRowCount(float width)
        {
            float requiredWidth = 0f;
            int availableCount = 0;
            for (int index = 0; index < buttons.Count; index++)
            {
                if (!buttons[index].Available)
                    continue;
                requiredWidth += buttons[index].MinimumWidth;
                availableCount++;
            }
            requiredWidth += Math.Max(0, availableCount - 1) * 2f;

            int maximumRows = Math.Max(1, Math.Min(MaximumRows, availableCount));
            return Math.Max(1, Math.Min(maximumRows,
                (int)Math.Ceiling(requiredWidth / Math.Max(1f, width))));
        }

        #endregion

        #region Selection and Buttons

        public void UpdateSelection()
        {
            for (int index = 0; index < buttons.Count; index++)
            {
                CategoryButton categoryButton = buttons[index];
                bool selected = categoryButton.Category == filter.Category;
                categoryButton.Button.Color = selected ? UiTheme.Selected : UiTheme.Panel;
                categoryButton.Button.Format = selected
                    ? GlyphFormat.White.WithColor(UiTheme.SelectedText).WithAlignment(TextAlignment.Center).WithSize(.68f)
                    : GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.68f);
                categoryButton.Button.HighlightEnabled = !selected;
            }
        }

        private void Add(BrowseCategory category)
        {
            string name = CatalogText.GetCategoryName(category);
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
                if (filter.Category == category)
                    return;
                filter.Category = category;
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
            for (int row = 0; row < rows.Length; row++)
                rows[row].Clear();

            float total = 0f;
            int availableCount = 0;
            for (int index = 0; index < buttons.Count; index++)
            {
                if (!buttons[index].Available)
                    continue;
                total += buttons[index].MinimumWidth;
                availableCount++;
            }
            float target = total / rowCount;
            int currentRow = 0;
            float currentWidth = 0f;

            for (int index = 0; index < buttons.Count; index++)
            {
                CategoryButton button = buttons[index];
                if (!button.Available)
                    continue;
                int buttonsRemaining = availableCount;
                int rowsRemaining = rowCount - currentRow;
                if (currentRow < rowCount - 1 && currentWidth > 0f &&
                    currentWidth + button.MinimumWidth > target && buttonsRemaining >= rowsRemaining)
                {
                    currentRow++;
                    currentWidth = 0f;
                }
                rows[currentRow].Add(button.Button, button.MinimumWidth);
                currentWidth += button.MinimumWidth;
                availableCount--;
            }

            activeRows = rowCount;
            for (int row = 0; row < rows.Length; row++)
                rows[row].Visible = row < rowCount;
        }

        #endregion
    }
}
