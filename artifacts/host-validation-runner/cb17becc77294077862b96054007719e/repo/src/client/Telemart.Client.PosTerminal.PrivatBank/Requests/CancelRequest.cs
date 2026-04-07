using Telemart.Client.PosTerminal.PrivatBank.Entity;

namespace Telemart.Client.PosTerminal.PrivatBank.Requests
{
    public class CancelRequest : RequestBase<CancelRequestItem, PurchaseResponseItem>
    {
        public CancelRequest(string invoiceNumber)
            : base("Withdrawal", 0, new CancelRequestItem(invoiceNumber))
        {
        }
    }
}