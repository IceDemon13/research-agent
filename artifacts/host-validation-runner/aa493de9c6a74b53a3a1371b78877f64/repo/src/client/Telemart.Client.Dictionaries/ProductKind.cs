namespace Telemart.Client.Dictionaries
{
    public sealed class ProductKind : DictionaryItem
    {
        private ProductKind(int id, string name, string title)
            : base(id, name, true)
        {
            Title = title;
        }

        public static ProductKind New { get; } = new ProductKind(1, "new", "Официал");

        public static ProductKind No { get; } = new ProductKind(2, "no", "Неофициал");

        public static ProductKind Ref { get; } = new ProductKind(3, "ref", "Ref/БУ");

        public static ProductKind Service { get; } = new ProductKind(4, "service", "Услуга");

        public string Title { get; }
    }
}