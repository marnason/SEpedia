using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DefinitionHeader
    {
        #region State and Construction

        private readonly HudChain textChain;
        private readonly Label title;
        private readonly Label description;
        private readonly MouseInputElement titleInput;

        public readonly HudChain Root;

        public DefinitionHeader()
        {
            title = new Label
            {
                Height = 36f,
                AutoResize = false,
                VertCenterText = true,
                Padding = new Vector2(8f, 4f)
            };
            titleInput = new MouseInputElement(title);

            description = new Label
            {
                BuilderMode = TextBuilderModes.Wrapped,
                AutoResize = true,
                VertCenterText = false,
                Padding = new Vector2(18f, 4f),
                Visible = false
            };

            textChain = new HudChain(true)
            {
                ParentAlignment = ParentAlignments.InnerTop,
                SizingMode = HudChainSizingModes.FitChainAlignAxis |
                    HudChainSizingModes.FitMembersOffAxis |
                    HudChainSizingModes.AlignMembersStart,
                Spacing = 1f,
                CollectionContainer = { title, description }
            };

            Root = new HudChain(false)
            {
                SizingMode = HudChainSizingModes.FitChainOffAxis |
                    HudChainSizingModes.AlignMembersStart,
                Spacing = 8f
            };
            Root.Add(textChain, 1f);
        }

        #endregion

        #region Content

        public void Update(
            string text,
            string id,
            string type,
            string descriptionText)
        {
            title.Text = new RichText(text ?? string.Empty, GlyphFormat.White.WithSize(1.25f));
            titleInput.ToolTip = "ID: " + (id ?? string.Empty) + "\nType: " + (type ?? string.Empty);

            description.Visible = !string.IsNullOrWhiteSpace(descriptionText);
            description.Text = new RichText(
                descriptionText ?? string.Empty,
                GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.82f));
        }

        #endregion

        #region Layout

        public void SetWidth(float width)
        {
            Root.Width = width;
            float textWidth = Math.Max(80f, width);
            textChain.Width = textWidth;
            title.Width = textWidth;
            description.Width = textWidth;
            description.LineWrapWidth = Math.Max(60f, textWidth - description.Padding.X);
        }

        #endregion
    }
}
