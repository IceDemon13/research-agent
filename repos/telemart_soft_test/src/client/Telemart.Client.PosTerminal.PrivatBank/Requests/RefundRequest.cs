using Telemart.Client.PosTerminal.PrivatBank.Entity;

namespace Telemart.Client.PosTerminal.PrivatBank.Requests
{
    public class RefundRequest : RequestBase<RefundRequestItem, PurchaseResponseItem>
    {
        public RefundRequest(decimal amount, decimal discount, string merchantId, string rrn)
            : base("Refund", 0, new RefundRequestItem(amount.ToString("f2"), discount.ToString("f2"), merchantId, rrn))
        {
        }
    }
}