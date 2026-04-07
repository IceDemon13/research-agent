namespace Telemart.Client.ViewModels.Store.Order
{
    public class FillSourcesWarehousesListParameter
    {
        public FillSourcesWarehousesListParameter(int orderId, int? orderWarehouseId)
        {
            OrderId = orderId;
            OrderWarehouseId = orderWarehouseId;
        }

        public int? OrderWarehouseId { get; }

        public int OrderId { get; }
    }
}
