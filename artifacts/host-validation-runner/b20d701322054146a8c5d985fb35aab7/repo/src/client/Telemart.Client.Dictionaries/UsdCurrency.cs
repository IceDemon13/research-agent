namespace Telemart.Client.Dictionaries
{
    public sealed class UsdCurrency
    {
        private UsdCurrency(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public static UsdCurrency Minus { get; } = new UsdCurrency(4, "USD-");

        public static UsdCurrency Plus { get; } = new UsdCurrency(5, "USD+");

        public int Id { get; }

        public string Title { get; }
    }
}