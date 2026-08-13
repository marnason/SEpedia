using SEpedia.Core;
using VRage.Game;

namespace SEpedia.UI
{
    public sealed class NavigationController
    {
        private readonly DefinitionIndex index;
        private readonly DefinitionList list;
        private readonly DefinitionView view;
        private bool navigating;

        public NavigationController(DefinitionIndex index, DefinitionList list, DefinitionView view)
        {
            this.index = index;
            this.list = list;
            this.view = view;

            list.SelectionChanged += OnListSelectionChanged;
            view.LinkClicked += NavigateTo;
        }

        public void NavigateTo(MyDefinitionId id)
        {
            DefinitionDocument definition;
            if (index.TryGet(id, out definition))
                NavigateTo(definition, true);
        }

        public void NavigateTo(DefinitionDocument definition, bool synchronizeList)
        {
            if (definition == null || navigating)
                return;

            navigating = true;
            try
            {
                view.Show(definition);
                if (synchronizeList)
                    list.TrySelect(definition);
            }
            finally
            {
                navigating = false;
            }
        }

        public void Close()
        {
            list.SelectionChanged -= OnListSelectionChanged;
            view.LinkClicked -= NavigateTo;
        }

        private void OnListSelectionChanged(CatalogEntry entry)
        {
            if (entry == null)
                return;

            if (entry.Definition != null)
                NavigateTo(entry.Definition, false);
            else
                view.Show(entry);
        }
    }
}
