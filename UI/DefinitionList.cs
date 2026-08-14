using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    public sealed class DefinitionList : HudElementBase
    {
        private sealed class CategoryButton
        {
            public readonly BrowseCategory Category;
            public readonly LabelBoxButton Button;
            public readonly float WidthWeight;

            public CategoryButton(BrowseCategory category, LabelBoxButton button, float widthWeight)
            {
                Category = category;
                Button = button;
                WidthWeight = widthWeight;
            }
        }

        public event Action<CatalogEntry> SelectionChanged;
        public event Action FilterRequested;
        public event Action ResultsChanged;

        private readonly DefinitionIndex definitions;
        private readonly CatalogFilter filter;
        private readonly List<CategoryButton> categoryButtons;
        private readonly HudChain categoryPanel;
        private readonly HudChain categoryRowOne;
        private readonly HudChain categoryRowTwo;
        private readonly ListBox<CatalogEntry> list;
        private readonly Label status;
        private CatalogIndex catalog;
        private CatalogResult currentResults;
        private bool updating;
        private bool compactCategories;

        public CatalogEntry First
        {
            get
            {
                return currentResults != null && currentResults.Items.Count > 0
                    ? currentResults.Items[0]
                    : null;
            }
        }

        public CatalogFilter Filter
        {
            get { return filter; }
        }

        public CatalogResult CurrentResults
        {
            get { return currentResults; }
        }

        public HudElementBase CategoryBar
        {
            get { return categoryPanel; }
        }

        public DefinitionList(
            DefinitionIndex definitions,
            CatalogFilter filter,
            IEnumerable<PlanetSnapshot> planets,
            HudParentBase parent = null) : base(parent)
        {
            this.definitions = definitions;
            this.filter = filter;
            catalog = new CatalogIndex(definitions, planets);
            categoryButtons = new List<CategoryButton>();

            categoryPanel = new HudChain(true)
            {
                SizingMode = HudChainSizingModes.FitChainAlignAxis | HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f
            };
            categoryRowOne = CreateCategoryRow();
            categoryRowTwo = CreateCategoryRow();
            categoryPanel.Add(categoryRowOne);
            categoryPanel.Add(categoryRowTwo);
            AddCategoryButton(BrowseCategory.Components, 1.25f);
            AddCategoryButton(BrowseCategory.Ores, .65f);
            AddCategoryButton(BrowseCategory.Ingots, .75f);
            AddCategoryButton(BrowseCategory.Ammo, .7f);
            AddCategoryButton(BrowseCategory.ToolsAndWeapons, 1.65f);
            AddCategoryButton(BrowseCategory.Consumables, 1.3f);
            AddCategoryButton(BrowseCategory.GasBottles, 1.15f);
            AddCategoryButton(BrowseCategory.Items, .65f);
            AddCategoryButton(BrowseCategory.Blocks, .75f);
            AddCategoryButton(BrowseCategory.Recipes, .85f);
            AddCategoryButton(BrowseCategory.Celestial, .95f);
            RebuildCategoryRows(false);
            UpdateCategoryButtons();

            var filterButton = new LabelBoxButton
            {
                Text = new RichText("Advanced filters", GlyphFormat.White.WithSize(.8f)),
                Height = 29f,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = Vector2.Zero,
                Color = new Color(36, 47, 55),
                HighlightColor = new Color(67, 82, 92)
            };
            filterButton.MouseInput.LeftClicked += delegate
            {
                Action handler = FilterRequested;
                if (handler != null)
                    handler();
            };

            list = new ListBox<CatalogEntry>
            {
                DimAlignment = DimAlignments.Width,
                Format = GlyphFormat.White.WithSize(.85f),
                LineHeight = 27f,
                MemberPadding = new Vector2(24f, 4f),
                UpdateValueCallback = OnSelectionChanged
            };

            status = new Label
            {
                Height = 24f,
                AutoResize = false,
                VertCenterText = true,
                Format = GlyphFormat.Blueish.WithSize(.72f),
                Padding = new Vector2(8f, 0f)
            };

            new HudChain(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f,
                CollectionContainer = { filterButton, { list, 1f }, status }
            };

            Refresh();
        }

        public void SetSearchText(string query)
        {
            filter.SearchText = query ?? string.Empty;
            Refresh();
        }

        public void Refresh()
        {
            string previousKey = list.Value != null && list.Value.AssocMember != null
                ? list.Value.AssocMember.StableKey
                : string.Empty;

            currentResults = catalog.Query(filter, 500);
            updating = true;
            try
            {
                list.ClearEntries();
                int selectedIndex = -1;
                for (int itemIndex = 0; itemIndex < currentResults.Items.Count; itemIndex++)
                {
                    CatalogEntry entry = currentResults.Items[itemIndex];
                    var text = new RichText();
                    text.Add(entry.DisplayName, GlyphFormat.White.WithSize(.83f));
                    text.Add("  " + GetEntryLabel(entry), GlyphFormat.Blueish.WithSize(.65f));
                    list.Add(text, entry);
                    if (entry.StableKey == previousKey)
                        selectedIndex = itemIndex;
                }

                if (currentResults.Items.Count > 0)
                    list.SetSelectionAt(selectedIndex >= 0 ? selectedIndex : 0);
            }
            finally
            {
                updating = false;
            }

            string categoryName = CatalogIndex.GetCategoryName(filter.Category).ToLowerInvariant();
            status.Text = currentResults.TotalCount > currentResults.Items.Count
                ? "Showing " + currentResults.Items.Count + " of " + currentResults.TotalCount + " " + categoryName
                : currentResults.TotalCount + " " + categoryName;

            if (list.Value != null)
                RaiseSelectionChanged(list.Value.AssocMember);

            Action resultsHandler = ResultsChanged;
            if (resultsHandler != null)
                resultsHandler();
        }

        public void RebuildCatalog(IEnumerable<PlanetSnapshot> planets)
        {
            catalog = new CatalogIndex(definitions, planets);
            Refresh();
        }

        public void UpdateCategoryLayout(float availableWidth)
        {
            bool compact = availableWidth < 1050f;
            if (compact != compactCategories)
                RebuildCategoryRows(compact);
        }

        public bool TrySelect(DefinitionDocument definition)
        {
            if (definition == null)
                return false;

            for (int index = 0; index < list.EntryList.Count; index++)
            {
                CatalogEntry entry = list.EntryList[index].AssocMember;
                if (entry.Definition != null && entry.Definition.Id == definition.Id)
                {
                    list.SetSelectionAt(index);
                    return true;
                }
            }
            return false;
        }

        private void AddCategoryButton(BrowseCategory category, float widthWeight)
        {
            LabelBoxButton button = CreateCategoryButton(category);
            categoryButtons.Add(new CategoryButton(category, button, widthWeight));
        }

        private static HudChain CreateCategoryRow()
        {
            return new HudChain(false)
            {
                Height = 29f,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f
            };
        }

        private void RebuildCategoryRows(bool compact)
        {
            categoryRowOne.Clear();
            categoryRowTwo.Clear();
            compactCategories = compact;

            int split = compact ? (categoryButtons.Count + 1) / 2 : categoryButtons.Count;
            for (int index = 0; index < categoryButtons.Count; index++)
            {
                CategoryButton category = categoryButtons[index];
                HudChain row = index < split ? categoryRowOne : categoryRowTwo;
                row.Add(category.Button, category.WidthWeight);
            }

            categoryRowTwo.Visible = compact;
        }

        private LabelBoxButton CreateCategoryButton(BrowseCategory category)
        {
            var button = new LabelBoxButton
            {
                Text = new RichText(
                    CatalogIndex.GetCategoryName(category),
                    GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.68f)),
                Height = 29f,
                AutoResize = false,
                VertCenterText = true,
                TextPadding = new Vector2(4f, 0f),
                Color = new Color(36, 47, 55),
                HighlightColor = new Color(67, 82, 92)
            };
            button.MouseInput.LeftClicked += delegate
            {
                if (filter.Category == category)
                    return;

                filter.Category = category;
                UpdateCategoryButtons();
                Refresh();
            };
            return button;
        }

        private void UpdateCategoryButtons()
        {
            for (int index = 0; index < categoryButtons.Count; index++)
            {
                CategoryButton categoryButton = categoryButtons[index];
                bool selected = categoryButton.Category == filter.Category;
                categoryButton.Button.Color = selected
                    ? new Color(142, 188, 206)
                    : new Color(36, 47, 55);
                categoryButton.Button.Format = selected
                    ? GlyphFormat.White.WithColor(new Color(39, 49, 55)).WithAlignment(TextAlignment.Center).WithSize(.68f)
                    : GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.68f);
                categoryButton.Button.HighlightEnabled = !selected;
            }
        }

        private void OnSelectionChanged(object sender, EventArgs args)
        {
            if (!updating && list.Value != null)
                RaiseSelectionChanged(list.Value.AssocMember);
        }

        private void RaiseSelectionChanged(CatalogEntry entry)
        {
            Action<CatalogEntry> handler = SelectionChanged;
            if (handler != null)
                handler(entry);
        }

        private static string GetEntryLabel(CatalogEntry entry)
        {
            if (entry.IsSpawnedPlanet)
                return "Spawned planet";
            if (entry.Definition.AsteroidGenerator != null)
                return "Asteroid generator";
            if (entry.Definition.PlanetGenerator != null)
                return "Planet definition";
            if (entry.Category == BrowseCategory.Recipes)
                return string.IsNullOrWhiteSpace(entry.ListDetail) ? "Recipe" : entry.ListDetail;
            switch (entry.Category)
            {
                case BrowseCategory.Components: return "Component";
                case BrowseCategory.Ores: return "Ore";
                case BrowseCategory.Ingots: return "Ingot";
                case BrowseCategory.Ammo: return "Ammo";
                case BrowseCategory.ToolsAndWeapons: return "Tool / weapon";
                case BrowseCategory.Consumables: return "Consumable";
                case BrowseCategory.GasBottles: return "Gas bottle";
                case BrowseCategory.Items: return "Item";
                case BrowseCategory.Blocks: return "Block";
                default: return "Entry";
            }
        }
    }
}
