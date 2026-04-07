using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class CloseInvoice : CallEntityActionRequestResultBase<InvoiceDto>
    {
        public CloseInvoice(int invoiceId)
            : base(invoiceId, "invoices", "close")
        {
        }
    }
}