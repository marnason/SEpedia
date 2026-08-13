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
        public event Action<CatalogEntry> SelectionChanged;
        public event Action FilterRequested;
        public event Action ResultsChanged;

        private readonly DefinitionIndex definitions;
        private readonly CatalogFilter filter;
        private readonly ListBox<BrowseCategory> categories;
        private readonly ListBox<CatalogEntry> list;
        private readonly Label status;
        private CatalogIndex catalog;
        private CatalogResult currentResults;
        private bool updating;

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

        public DefinitionList(
            DefinitionIndex definitions,
            CatalogFilter filter,
            IEnumerable<PlanetSnapshot> planets,
            HudParentBase parent = null) : base(parent)
        {
            this.definitions = definitions;
            this.filter = filter;
            catalog = new CatalogIndex(definitions, planets);

            categories = new ListBox<BrowseCategory>
            {
                Height = 236f,
                DimAlignment = DimAlignments.Width,
                Format = GlyphFormat.White.WithSize(.78f),
                LineHeight = 22f,
                MemberPadding = new Vector2(10f, 2f),
                UpdateValueCallback = OnCategoryChanged
            };

            updating = true;

            BrowseCategory[] orderedCategories =
            {
                BrowseCategory.Components,
                BrowseCategory.Ores,
                BrowseCategory.Ingots,
                BrowseCategory.Ammo,
                BrowseCategory.ToolsAndWeapons,
                BrowseCategory.Consumables,
                BrowseCategory.GasBottles,
                BrowseCategory.Items,
                BrowseCategory.Blocks,
                BrowseCategory.Celestial
            };

            for (int index = 0; index < orderedCategories.Length; index++)
            {
                BrowseCategory category = orderedCategories[index];
                categories.Add(new RichText(CatalogIndex.GetCategoryName(category), GlyphFormat.White.WithSize(.78f)), category);
                if (category == filter.Category)
                    categories.SetSelectionAt(index);
            }
            updating = false;

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
                MemberPadding = new Vector2(12f, 4f),
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
                CollectionContainer = { categories, filterButton, { list, 1f }, status }
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

        private void OnCategoryChanged(object sender, EventArgs args)
        {
            if (updating || categories.Value == null)
                return;

            filter.Category = categories.Value.AssocMember;
            Refresh();
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
