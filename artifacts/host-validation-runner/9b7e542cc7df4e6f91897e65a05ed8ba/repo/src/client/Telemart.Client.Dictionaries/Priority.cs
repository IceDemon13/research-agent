namespace Telemart.Client.Dictionaries
{
    public sealed class Priority : DictionaryItem
    {
        private Priority(int id, string name, int weight)
            : base(id, name, true)
        {
            Weight = weight;
        }

        public static Priority Low { get; } = new Priority(1, "Низкий", 10);

        public static Priority Normal { get; } = new Priority(2, "Обычный", 20);

        public static Priority High { get; } = new Priority(3, "Высокий", 30);

        public static Priority Critical { get; } = new Priority(4, "Критичный", 40);

        public int Weight { get; }
    }
}
