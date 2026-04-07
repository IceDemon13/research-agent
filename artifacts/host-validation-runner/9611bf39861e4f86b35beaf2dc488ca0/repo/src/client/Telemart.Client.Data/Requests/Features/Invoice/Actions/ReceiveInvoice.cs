using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class ReceiveInvoice : CallEntityActionRequestResultBase<InvoiceDto>
    {
        public ReceiveInvoice(int invoiceId)
            : base(invoiceId, ApiResources.Invoices, "receive")
        {
        }
    }
}