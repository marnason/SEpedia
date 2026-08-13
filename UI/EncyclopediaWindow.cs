using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    public sealed class EncyclopediaWindow : WindowBase
    {
        private readonly TextField searchField;
        private readonly DefinitionList definitionList;
        private readonly DefinitionView definitionView;
        private readonly NavigationController navigation;

        public EncyclopediaWindow(DefinitionIndex index, HudParentBase parent = null) : base(parent)
        {
            HeaderText = new RichText("SEpedia", GlyphFormat.White.WithAlignment(TextAlignment.Left).WithSize(1.08f));
            header.TextPadding = new Vector2(14f, 0f);

            searchField = new TextField(header)
            {
                Text = string.Empty,
                Width = 310f,
                Height = 25f,
                ParentAlignment = ParentAlignments.InnerRight,
                AutoResize = false,
                Format = GlyphFormat.White.WithSize(.82f),
                UpdateValueCallback = SearchChanged
            };

            definitionList = new DefinitionList(index)
            {
                Width = 335f
            };

            definitionView = new DefinitionView(index);

            new HudChain(false, body)
            {
                DimAlignment = DimAlignments.UnpaddedSize,
                SizingMode = HudChainSizingModes.FitMembersOffAxis,
                Spacing = 2f,
                CollectionContainer = { definitionList, { definitionView, 1f } }
            };

            navigation = new NavigationController(index, definitionList, definitionView);

            BodyColor = new Color(31, 40, 47, 235);
            BorderColor = new Color(58, 68, 77);
            MinimumSize = new Vector2(720f, 430f);
            Size = new Vector2(1000f, 650f);
            MouseInput.RequestCursor = true;
            Visible = false;

            if (definitionList.First != null)
                navigation.NavigateTo(definitionList.First, true);
        }

        public void Toggle()
        {
            Visible = !Visible;

            if (Visible)
                GetWindowFocus();
            else
                searchField.CloseInput();
        }

        public void Close()
        {
            navigation.Close();
            Unregister();
        }

        protected override void Layout()
        {
            base.Layout();
            searchField.Width = Math.Min(360f, Math.Max(220f, Width * .36f));
            definitionList.Width = Math.Min(380f, Math.Max(280f, body.Width * .34f));
        }

        private void SearchChanged(object sender, EventArgs args)
        {
            definitionList.Refresh(searchField.Value.ToString());
        }
    }
}
