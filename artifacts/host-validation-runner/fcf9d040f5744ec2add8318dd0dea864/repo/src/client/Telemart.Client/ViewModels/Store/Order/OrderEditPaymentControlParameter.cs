namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderEditPaymentControlParameter : OrderEditPaymentParameter
    {
        public OrderEditPaymentControlParameter(int orderId, int subdivisionId, int paymentId, decimal amount, int? selectedPaymentId)
            : base(orderId, subdivisionId, paymentId, amount, selectedPaymentId)
        {
        }
    }
}