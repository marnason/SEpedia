using System.Collections.Generic;
using RichHudFramework.UI;
using VRageMath;

namespace SEpedia.UI
{
    internal sealed class FilterSectionPanel
    {
        public readonly HudChain Root;
        public readonly ScrollBoxEntry Entry;

        public FilterSectionPanel(
            ScrollBox content,
            IList<ScrollBoxEntry> group = null)
        {
            Root = new HudChain(true)
            {
                SizingMode = HudChainSizingModes.FitChainAlignAxis |
                    HudChainSizingModes.FitMembersOffAxis,
                Padding = new Vector2(6f, 6f),
                Spacing = 2f
            };

            UiTheme.AddGroupPanelBackdrop(Root);

            Entry = new ScrollBoxEntry();
            Entry.SetElement(Root);
            content.Add(Entry);
            if (group != null)
                group.Add(Entry);
        }

        public void Add(HudElementBase row)
        {
            Root.Add(row);
        }
    }
}
