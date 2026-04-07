namespace Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo
{
    public interface IOrderPayment
    {
        int CurrencyId { get; }

        sbyte Sign { get; }

        decimal Amount { get; }

        int? PaymentId { get; }
    }
}
