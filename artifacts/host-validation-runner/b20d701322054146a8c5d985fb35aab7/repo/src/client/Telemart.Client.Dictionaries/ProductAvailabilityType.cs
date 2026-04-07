namespace Telemart.Client.Dictionaries
{
    public sealed class ProductAvailabilityType : DictionaryItem
    {
        private ProductAvailabilityType(int id, string name)
            : base(id, name, true)
        {
        }

        public static ProductAvailabilityType InStock { get; } = new ProductAvailabilityType(1, "Есть в наличии");

        public static ProductAvailabilityType Expected { get; } = new ProductAvailabilityType(2, "Ожидается");

        public static ProductAvailabilityType Unknown { get; } = new ProductAvailabilityType(3, "Достоверно неизвестно");

        public static ProductAvailabilityType NotInStock { get; } = new ProductAvailabilityType(4, "Нет в наличии");
    }
}