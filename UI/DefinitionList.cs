using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    public sealed class DefinitionList : HudElementBase
    {
        public event Action<DefinitionDocument> SelectionChanged;

        private readonly DefinitionIndex index;
        private readonly ListBox<DefinitionDocument> list;
        private readonly Label status;
        private SearchResult currentResults;

        public DefinitionDocument First
        {
            get
            {
                return currentResults != null && currentResults.Items.Count > 0
                    ? currentResults.Items[0]
                    : null;
            }
        }

        public DefinitionList(DefinitionIndex index, HudParentBase parent = null) : base(parent)
        {
            this.index = index;

            var title = new Label
            {
                Text = new RichText("Definitions", GlyphFormat.White.WithSize(1.05f)),
                Height = 28f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(8f, 0f)
            };

            list = new ListBox<DefinitionDocument>
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
                Format = GlyphFormat.Blueish.WithSize(.75f),
                Padding = new Vector2(8f, 0f)
            };

            new HudChain(this)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                CollectionContainer = { title, { list, 1f }, status }
            };

            Refresh(string.Empty);
        }

        public void Refresh(string query)
        {
            currentResults = index.Search.Search(query, 500);
            list.ClearEntries();

            for (int itemIndex = 0; itemIndex < currentResults.Items.Count; itemIndex++)
            {
                DefinitionDocument definition = currentResults.Items[itemIndex];
                var text = new RichText();
                text.Add(definition.DisplayName, GlyphFormat.White.WithSize(.85f));
                text.Add("  " + GetCategoryLabel(definition), GlyphFormat.Blueish.WithSize(.68f));
                list.Add(text, definition);
            }

            status.Text = currentResults.TotalCount > currentResults.Items.Count
                ? "Showing " + currentResults.Items.Count + " of " + currentResults.TotalCount + "; refine search"
                : currentResults.TotalCount + " definitions";
        }

        public bool TrySelect(DefinitionDocument definition)
        {
            if (definition == null)
                return false;

            for (int itemIndex = 0; itemIndex < list.EntryList.Count; itemIndex++)
            {
                if (list.EntryList[itemIndex].AssocMember.Id == definition.Id)
                {
                    list.SetSelectionAt(itemIndex);
                    return true;
                }
            }

            return false;
        }

        private void OnSelectionChanged(object sender, EventArgs args)
        {
            if (list.Value != null && SelectionChanged != null)
                SelectionChanged(list.Value.AssocMember);
        }

        private static string GetCategoryLabel(DefinitionDocument definition)
        {
            if ((definition.Categories & DefinitionCategory.Component) != 0)
                return "Component";
            if ((definition.Categories & DefinitionCategory.Ore) != 0)
                return "Ore";
            if ((definition.Categories & DefinitionCategory.Ingot) != 0)
                return "Ingot";
            if ((definition.Categories & DefinitionCategory.CubeBlock) != 0)
                return "Block";
            if ((definition.Categories & DefinitionCategory.Blueprint) != 0)
                return "Recipe";
            if ((definition.Categories & DefinitionCategory.PhysicalItem) != 0)
                return "Item";

            return "Definition";
        }
    }
}
