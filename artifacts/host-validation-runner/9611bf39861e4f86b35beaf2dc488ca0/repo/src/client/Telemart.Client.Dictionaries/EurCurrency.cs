namespace Telemart.Client.Dictionaries
{
    public sealed class EurCurrency
    {
        private EurCurrency(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public static EurCurrency Standart { get; } = new EurCurrency(1, "EUR");

        public int Id { get; }

        public string Title { get; }
    }
}