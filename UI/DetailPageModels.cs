using System.Collections.Generic;
using VRage.Game;

namespace SEpedia.UI
{
    internal enum DetailRowKind
    {
        Heading,
        Field,
        PagedSection
    }

    internal sealed class DetailItem
    {
        public string Text { get; private set; }
        public MyDefinitionId? LinkId { get; private set; }

        public DetailItem(string text, MyDefinitionId? linkId = null)
        {
            Text = text ?? string.Empty;
            LinkId = linkId;
        }
    }

    internal sealed class DetailRowModel
    {
        #region State

        private static readonly IReadOnlyList<DetailItem> EmptyItems =
            new List<DetailItem>().AsReadOnly();

        public DetailRowKind Kind { get; private set; }
        public string Label { get; private set; }
        public string Value { get; private set; }
        public bool Major { get; private set; }
        public IReadOnlyList<DetailItem> Items { get; private set; }

        #endregion

        #region Construction

        private DetailRowModel(
            DetailRowKind kind,
            string label,
            string value,
            bool major,
            IList<DetailItem> items)
        {
            Kind = kind;
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Major = major;
            Items = items != null
                ? new List<DetailItem>(items).AsReadOnly()
                : EmptyItems;
        }

        #endregion

        #region Row Factories

        public static DetailRowModel Heading(string text)
        {
            return new DetailRowModel(DetailRowKind.Heading, text, null, true, null);
        }

        public static DetailRowModel Field(string label, string value)
        {
            return new DetailRowModel(DetailRowKind.Field, label, value, false, null);
        }

        public static DetailRowModel Paged(string heading, IList<DetailItem> items, bool major)
        {
            return new DetailRowModel(DetailRowKind.PagedSection, heading, null, major, items);
        }

        #endregion
    }

    internal sealed class DetailPageModel
    {
        #region State

        public string Title { get; private set; }
        public string Id { get; private set; }
        public string RuntimeType { get; private set; }
        public string Description { get; private set; }
        public IReadOnlyList<DetailRowModel> Rows { get; private set; }

        #endregion

        #region Construction

        public DetailPageModel(
            string title,
            string id,
            string runtimeType,
            string description,
            IList<DetailRowModel> rows)
        {
            Title = title ?? string.Empty;
            Id = id ?? string.Empty;
            RuntimeType = runtimeType ?? string.Empty;
            Description = description ?? string.Empty;
            Rows = new List<DetailRowModel>(rows).AsReadOnly();
        }

        #endregion
    }
}
