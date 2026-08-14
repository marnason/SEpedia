using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using SEpedia.Core;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class DefinitionHeader
    {
        private const int IconLayerLimit = 8;
        private const int IconSize = 44;

        private readonly EmptyHudElement iconArea;
        private readonly TexturedBox[] iconLayers;
        private readonly Dictionary<string, Material> materialCache;
        private readonly HudChain textChain;
        private readonly Label title;
        private readonly Label description;
        private readonly MouseInputElement titleInput;
        private bool hasIcon;

        public readonly HudChain Root;

        public DefinitionHeader()
        {
            iconArea = new EmptyHudElement
            {
                Width = IconSize,
                Height = IconSize,
                ParentAlignment = ParentAlignments.InnerTop,
                Visible = false
            };
            iconLayers = new TexturedBox[IconLayerLimit];
            materialCache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < iconLayers.Length; index++)
            {
                iconLayers[index] = new TexturedBox(iconArea)
                {
                    DimAlignment = DimAlignments.UnpaddedSize,
                    MatAlignment = MaterialAlignment.StretchToFit,
                    Color = Color.White,
                    Visible = false
                };
            }

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
            Root.Add(iconArea);
            Root.Add(textChain, 1f);
        }

        public void Update(
            string text,
            string id,
            string type,
            string descriptionText,
            DefinitionIconData icon)
        {
            title.Text = new RichText(text ?? string.Empty, GlyphFormat.White.WithSize(1.25f));
            titleInput.ToolTip = "ID: " + (id ?? string.Empty) + "\nType: " + (type ?? string.Empty);

            description.Visible = !string.IsNullOrWhiteSpace(descriptionText);
            description.Text = new RichText(
                descriptionText ?? string.Empty,
                GlyphFormat.White.WithAlignment(TextAlignment.Center).WithSize(.82f));

            IReadOnlyList<string> materialIds = icon != null && icon.IsRenderable
                ? icon.MaterialIds
                : null;
            int layerCount = materialIds != null ? materialIds.Count : 0;
            hasIcon = layerCount > 0 && layerCount <= iconLayers.Length;
            iconArea.Visible = hasIcon;
            for (int index = 0; index < iconLayers.Length; index++)
            {
                bool visible = hasIcon && index < layerCount;
                iconLayers[index].Visible = visible;
                if (visible)
                    iconLayers[index].Material = GetMaterial(materialIds[index]);
            }
        }

        public void SetWidth(float width)
        {
            Root.Width = width;
            float textWidth = Math.Max(80f, width - (hasIcon ? iconArea.Width + Root.Spacing : 0f));
            textChain.Width = textWidth;
            title.Width = textWidth;
            description.Width = textWidth;
            description.LineWrapWidth = Math.Max(60f, textWidth - description.Padding.X);
        }

        private Material GetMaterial(string materialId)
        {
            Material material;
            if (!materialCache.TryGetValue(materialId, out material))
            {
                material = new Material(materialId, new Vector2(256f));
                materialCache.Add(materialId, material);
            }
            return material;
        }
    }
}
