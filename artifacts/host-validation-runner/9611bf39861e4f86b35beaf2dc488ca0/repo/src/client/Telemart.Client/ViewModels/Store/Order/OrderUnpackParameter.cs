namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderUnpackParameter
    {
        public OrderUnpackParameter(int orderId, int[] orderCellIds)
        {
            OrderId = orderId;
            OrderCellIds = orderCellIds;
        }

        public OrderUnpackParameter(int orderId, int orderCellId)
        {
            OrderId = orderId;
            OrderCellIds = [orderCellId];
        }

        public int OrderId { get; }

        public int[] OrderCellIds { get; }
    }
}
