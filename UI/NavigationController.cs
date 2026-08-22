using System;
using System.Collections.Generic;
using SEpedia.Core;
using VRage.Game;

namespace SEpedia.UI
{
    internal sealed class NavigationController
    {
        #region State and Construction

        public event Action HistoryChanged;

        private readonly DefinitionIndex index;
        private readonly DefinitionList list;
        private readonly DefinitionView view;
        private readonly List<CatalogEntry> history;
        private int historyIndex;
        private bool navigating;
        private bool closed;

        public bool CanGoPrevious
        {
            get { return historyIndex > 0; }
        }

        public bool CanGoNext
        {
            get { return historyIndex >= 0 && historyIndex < history.Count - 1; }
        }

        public NavigationController(DefinitionIndex index, DefinitionList list, DefinitionView view)
        {
            this.index = index;
            this.list = list;
            this.view = view;
            history = new List<CatalogEntry>();
            historyIndex = -1;

            list.SelectionChanged += OnListSelectionChanged;
            view.LinkClicked += NavigateTo;
        }

        #endregion

        #region Navigation

        public void NavigateTo(MyDefinitionId id)
        {
            DefinitionDocument definition;
            if (index.TryGet(id, out definition))
                NavigateTo(new CatalogEntry(definition, 0), true, true);
        }

        public void NavigateTo(CatalogEntry entry, bool synchronizeList)
        {
            NavigateTo(entry, synchronizeList, true);
        }

        private void NavigateTo(CatalogEntry entry, bool synchronizeList, bool recordDuplicate)
        {
            if (entry == null || navigating)
                return;

            if (!recordDuplicate && CurrentMatches(entry))
                return;

            if (historyIndex < history.Count - 1)
                history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);
            history.Add(entry);
            historyIndex = history.Count - 1;

            Show(entry, synchronizeList);
            RaiseHistoryChanged();
        }

        public void GoPrevious()
        {
            MoveHistory(-1);
        }

        public void GoNext()
        {
            MoveHistory(1);
        }

        private void MoveHistory(int delta)
        {
            int targetIndex = historyIndex + delta;
            if (navigating || targetIndex < 0 || targetIndex >= history.Count)
                return;

            historyIndex = targetIndex;
            Show(history[historyIndex], true);
            RaiseHistoryChanged();
        }

        private void Show(CatalogEntry entry, bool synchronizeList)
        {
            if (entry == null)
                return;

            navigating = true;
            try
            {
                view.Show(entry);
                if (synchronizeList)
                {
                    if (entry.Definition != null)
                        list.TryReveal(entry.Definition);
                    else
                        list.TrySelect(entry);
                }
            }
            finally
            {
                navigating = false;
            }
        }

        private bool CurrentMatches(CatalogEntry entry)
        {
            return historyIndex >= 0 && historyIndex < history.Count &&
                history[historyIndex].StableKey == entry.StableKey;
        }

        #endregion

        #region Lifecycle and Events

        public void Close()
        {
            if (closed)
                return;
            closed = true;
            list.SelectionChanged -= OnListSelectionChanged;
            view.LinkClicked -= NavigateTo;
            HistoryChanged = null;
        }

        private void OnListSelectionChanged(CatalogEntry entry, bool explicitSelection)
        {
            if (entry == null)
                return;

            NavigateTo(entry, false, explicitSelection);
        }

        private void RaiseHistoryChanged()
        {
            Action handler = HistoryChanged;
            if (handler != null)
                handler();
        }

        #endregion
    }
}
