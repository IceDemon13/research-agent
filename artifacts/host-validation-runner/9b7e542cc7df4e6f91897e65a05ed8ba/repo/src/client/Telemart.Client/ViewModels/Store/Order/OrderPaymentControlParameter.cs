namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderPaymentControlParameter
    {
        public OrderPaymentControlParameter(int orderId, ExternalPaymentViewItem externalPayment, int subdivisionId, decimal payAmount, decimal holdedAmount, decimal extraAmount)
        {
            OrderId = orderId;
            ExternalPayment = externalPayment;
            SubdivisionId = subdivisionId;
            ToPayAmount = payAmount;
            HoldedAmount = holdedAmount;
            ExtraAmount = extraAmount;
        }

        public int OrderId { get; }

        public int SubdivisionId { get; }

        public decimal ToPayAmount { get; }

        public decimal HoldedAmount { get; }

        public decimal ExtraAmount { get; }

        public ExternalPaymentViewItem ExternalPayment { get; set; }
    }
}