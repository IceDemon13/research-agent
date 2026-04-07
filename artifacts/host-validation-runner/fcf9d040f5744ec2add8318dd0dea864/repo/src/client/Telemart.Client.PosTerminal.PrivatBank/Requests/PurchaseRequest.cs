using Telemart.Client.PosTerminal.PrivatBank.Entity;

namespace Telemart.Client.PosTerminal.PrivatBank.Requests
{
    public class PurchaseRequest : RequestBase<PurchaseRequestItem, PurchaseResponseItem>
    {
        public PurchaseRequest(decimal amount, decimal discount, string merchantId)
            : base("Purchase", 0, new PurchaseRequestItem(amount.ToString("f2"), discount.ToString("f2"), merchantId))
        {
        }
    }
}
