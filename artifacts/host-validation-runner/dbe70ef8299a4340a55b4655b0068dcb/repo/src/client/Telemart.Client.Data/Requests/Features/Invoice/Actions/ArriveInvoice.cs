using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class ArriveInvoice : CallEntityActionRequestResultBase<InvoiceDto>
    {
        public ArriveInvoice(int invoiceId)
            : base(invoiceId, "invoices", "arrive")
        {
        }
    }
}