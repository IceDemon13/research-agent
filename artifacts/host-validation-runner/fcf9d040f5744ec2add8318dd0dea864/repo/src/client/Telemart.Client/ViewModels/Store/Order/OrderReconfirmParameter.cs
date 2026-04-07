namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderReconfirmParameter
    {
        public OrderReconfirmParameter(int orderId, int[] orderProductIds = null)
        {
            OrderId = orderId;
            OrderProductIds = orderProductIds;
        }

        public OrderReconfirmParameter(int orderId, int orderProductId)
        {
            OrderId = orderId;
            OrderProductIds = new[] { orderProductId };
        }

        public int OrderId { get; }

        public int[] OrderProductIds { get; }
    }
}
