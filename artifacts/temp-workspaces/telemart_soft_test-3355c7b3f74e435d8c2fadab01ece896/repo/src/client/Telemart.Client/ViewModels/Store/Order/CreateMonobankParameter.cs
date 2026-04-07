namespace Telemart.Client.ViewModels.Store.Order
{
    public class CreateMonobankParameter
    {
        public CreateMonobankParameter(int orderId, decimal orderSum, int partialPay, int paymentId)
        {
            OrderId = orderId;
            OrderSum = orderSum;
            PartialPay = partialPay;
            PaymentId = paymentId;
        }

        public int PartialPay { get; }

        public decimal OrderSum { get; }

        public int OrderId { get; }

        public int PaymentId { get; }
    }
}