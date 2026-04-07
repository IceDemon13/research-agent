namespace Telemart.Client.Dictionaries
{
    public sealed class Module : DictionaryItem
    {
        public const int ProductPricesId = 1;
        public const int OrdersId = 2;
        public const int CallsId = 3;

        public Module(int id, string name)
            : base(id, name, true)
        {
        }

        public static Module ProductPrices { get; } = new Module(ProductPricesId, "Цены");

        public static Module Orders { get; } = new Module(OrdersId, "Заказы");

        public static Module Calls { get; } = new Module(CallsId, "Звонки");

    }
}