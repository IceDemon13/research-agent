using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public class SwitchIgnoreTransitInvoice : CallEntityActionRequestResultBase<InvoiceDto>
    {
        public SwitchIgnoreTransitInvoice(int id)
            : base(id, ApiResources.Invoices, "switch_ignore_transit")
        {
        }
    }
}
