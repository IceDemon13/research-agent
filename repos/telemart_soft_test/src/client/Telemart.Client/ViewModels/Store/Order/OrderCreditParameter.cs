using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderCreditParameter
    {
        public OrderCreditParameter(int orderId, int priceTypeId)
        {
            OrderId = orderId;
            PriceTypeId = priceTypeId;
        }

        public int OrderId { get; init; }

        public int PriceTypeId { get; init; }
    }
}