namespace Telemart.Client.ViewModels.Store.Order
{
    public class CreateOrderBillParameter
    {
        public CreateOrderBillParameter(int orderId, int orderPaymentId)
        {
            OrderId = orderId;
            OrderPaymentId = orderPaymentId;
        }

        public int OrderId { get; private set; }

        public int OrderPaymentId { get; private set; }
    }
}
