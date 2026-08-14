using System.Collections.Generic;
using SEpedia.Core;

namespace SEpedia.UI
{
    internal enum DetailRowKind
    {
        Heading,
        Field,
        Paragraph,
        Link,
        PagedSection
    }

    internal sealed class DetailRowModel
    {
        public DetailRowKind Kind { get; private set; }
        public string Label { get; private set; }
        public string Value { get; private set; }
        public bool Major { get; private set; }
        public DetailItem Link { get; private set; }
        public IReadOnlyList<DetailItem> Items { get; private set; }

        private DetailRowModel(
            DetailRowKind kind,
            string label,
            string value,
            bool major,
            DetailItem link,
            IList<DetailItem> items)
        {
            Kind = kind;
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Major = major;
            Link = link;
            Items = items != null
                ? new List<DetailItem>(items).AsReadOnly()
                : new List<DetailItem>().AsReadOnly();
        }

        public static DetailRowModel Heading(string text)
        {
            return new DetailRowModel(DetailRowKind.Heading, text, null, true, null, null);
        }

        public static DetailRowModel Field(string label, string value)
        {
            return new DetailRowModel(DetailRowKind.Field, label, value, false, null, null);
        }

        public static DetailRowModel Paragraph(string text)
        {
            return new DetailRowModel(DetailRowKind.Paragraph, null, text, false, null, null);
        }

        public static DetailRowModel LinkRow(DetailItem item)
        {
            return new DetailRowModel(DetailRowKind.Link, null, null, false, item, null);
        }

        public static DetailRowModel Paged(string heading, IList<DetailItem> items, bool major)
        {
            return new DetailRowModel(DetailRowKind.PagedSection, heading, null, major, null, items);
        }
    }

    internal sealed class DetailPageModel
    {
        public string Title { get; private set; }
        public string Id { get; private set; }
        public string RuntimeType { get; private set; }
        public string Description { get; private set; }
        public DefinitionIconData Icon { get; private set; }
        public IReadOnlyList<DetailRowModel> Rows { get; private set; }

        public DetailPageModel(
            string title,
            string id,
            string runtimeType,
            string description,
            DefinitionIconData icon,
            IList<DetailRowModel> rows)
        {
            Title = title ?? string.Empty;
            Id = id ?? string.Empty;
            RuntimeType = runtimeType ?? string.Empty;
            Description = description ?? string.Empty;
            Icon = icon;
            Rows = new List<DetailRowModel>(rows).AsReadOnly();
        }
    }
}
