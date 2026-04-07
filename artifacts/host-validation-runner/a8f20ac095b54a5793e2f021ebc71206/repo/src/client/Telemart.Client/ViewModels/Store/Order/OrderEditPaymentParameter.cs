namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderEditPaymentParameter
    {
        public OrderEditPaymentParameter(int orderId, int subdivisionId, int paymentId, decimal orderTotalAmount, int? selectedPaymentId = null, bool changePayment = false)
        {
            OrderId = orderId;
            SubdivisionId = subdivisionId;
            PaymentId = paymentId;
            OrderTotalAmount = orderTotalAmount;
            SelectedPaymentId = selectedPaymentId;
            ChangePayment = changePayment;
        }

        public int OrderId { get; }

        public int SubdivisionId { get; }

        public int PaymentId { get; }

        public decimal OrderTotalAmount { get; }

        public int? SelectedPaymentId { get; }

        public bool ChangePayment { get; }
    }
}