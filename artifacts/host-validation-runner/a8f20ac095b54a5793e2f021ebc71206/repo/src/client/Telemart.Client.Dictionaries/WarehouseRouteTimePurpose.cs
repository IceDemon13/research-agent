namespace Telemart.Client.Dictionaries
{
    public sealed class WarehouseRouteTimePurpose : DictionaryItemBase
    {
        private const int OrdersId = 1;
        private const int ShowcaseId = 2;

        private WarehouseRouteTimePurpose(int id, string name)
            : base(id, name)
        {
        }


        public static WarehouseRouteTimePurpose Orders { get; } = new WarehouseRouteTimePurpose(OrdersId, "Заказы");

        public static WarehouseRouteTimePurpose Showcase { get; } = new WarehouseRouteTimePurpose(ShowcaseId, "Витрина");
    }
}