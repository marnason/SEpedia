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
        private sealed class CategoryButton
        {
            public readonly BrowseCategory Category;
            public readonly LabelBoxButton Button;
            public readonly float MinimumWidth;

            public CategoryButton(
                BrowseCategory category,
                LabelBoxButton button,
                float minimumWidth)
            {
                Category = category;
                Button = button;
                MinimumWidth = minimumWidth;
            }
        }

        private const int MaximumRows = 3;
        private readonly CatalogFilter filter;
        private readonly Action categoryChanged;
        private readonly List<CategoryButton> buttons;
        private readonly HudChain[] rows;
        private int activeRows;

        public HudChain Root { get; private set; }

        public CategoryBar(CatalogFilter filter, Action categoryChanged)
        {
            this.filter = filter;
            this.categoryChanged = categoryChanged;
            buttons = new List<CategoryButton>();
            rows = new HudChain[MaximumRows];

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
            Add(BrowseCategory.GasBottles);
            Add(BrowseCategory.Items);
            Add(BrowseCategory.Blocks);
            Add(BrowseCategory.Recipes);
            Add(BrowseCategory.Celestial);
            Reflow(1);
            UpdateSelection();
        }

        public void UpdateLayout(float availableWidth)
        {
            float requiredWidth = 0f;
            for (int index = 0; index < buttons.Count; index++)
                requiredWidth += buttons[index].MinimumWidth;
            requiredWidth += (buttons.Count - 1) * 2f;

            int rowCount = Math.Max(1, Math.Min(MaximumRows,
                (int)Math.Ceiling(requiredWidth / Math.Max(1f, availableWidth))));
            if (rowCount != activeRows)
                Reflow(rowCount);
        }

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

        private void Reflow(int rowCount)
        {
            for (int row = 0; row < rows.Length; row++)
                rows[row].Clear();

            float total = 0f;
            for (int index = 0; index < buttons.Count; index++)
                total += buttons[index].MinimumWidth;
            float target = total / rowCount;
            int currentRow = 0;
            float currentWidth = 0f;

            for (int index = 0; index < buttons.Count; index++)
            {
                CategoryButton button = buttons[index];
                int buttonsRemaining = buttons.Count - index;
                int rowsRemaining = rowCount - currentRow;
                if (currentRow < rowCount - 1 && currentWidth > 0f &&
                    currentWidth + button.MinimumWidth > target && buttonsRemaining >= rowsRemaining)
                {
                    currentRow++;
                    currentWidth = 0f;
                }
                rows[currentRow].Add(button.Button, button.MinimumWidth);
                currentWidth += button.MinimumWidth;
            }

            activeRows = rowCount;
            for (int row = 0; row < rows.Length; row++)
                rows[row].Visible = row < rowCount;
        }
    }
}
