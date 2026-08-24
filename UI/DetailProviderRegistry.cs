using System;
using System.Collections.Generic;
using SEpedia.Core;

namespace SEpedia.UI
{
    internal sealed class DetailCompositionContext
    {
        public CatalogEntry Entry { get; private set; }
        public IList<DetailRowModel> Rows { get; private set; }

        public DetailCompositionContext(CatalogEntry entry, IList<DetailRowModel> rows)
        {
            Entry = entry;
            Rows = rows;
        }
    }

    internal interface IDetailProvider
    {
        int Order { get; }
        bool AppliesTo(CatalogEntry entry);
        void Compose(DetailCompositionContext context);
    }

    internal sealed class DelegateDetailProvider : IDetailProvider
    {
        private readonly Func<CatalogEntry, bool> appliesTo;
        private readonly Action<DetailCompositionContext> compose;

        public int Order { get; private set; }

        public DelegateDetailProvider(
            int order,
            Func<CatalogEntry, bool> appliesTo,
            Action<DetailCompositionContext> compose)
        {
            Order = order;
            this.appliesTo = appliesTo;
            this.compose = compose;
        }

        public bool AppliesTo(CatalogEntry entry)
        {
            return appliesTo(entry);
        }

        public void Compose(DetailCompositionContext context)
        {
            compose(context);
        }
    }

    internal sealed class DetailProviderRegistry
    {
        private readonly List<IDetailProvider> providers;

        public DetailProviderRegistry()
        {
            providers = new List<IDetailProvider>();
        }

        public void Register(IDetailProvider provider)
        {
            providers.Add(provider);
            providers.Sort(delegate(IDetailProvider left, IDetailProvider right)
            {
                return left.Order.CompareTo(right.Order);
            });
        }

        public void Compose(CatalogEntry entry, IList<DetailRowModel> rows)
        {
            var context = new DetailCompositionContext(entry, rows);
            for (int index = 0; index < providers.Count; index++)
            {
                if (providers[index].AppliesTo(entry))
                    providers[index].Compose(context);
            }
        }
    }
}
