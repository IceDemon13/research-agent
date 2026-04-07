using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class DontReceiveInvoice : CallEntityActionRequestResultBase<InvoiceDto>
    {
        public DontReceiveInvoice(int invoiceId)
            : base(invoiceId, ApiResources.Invoices, "dontreceive")
        {
        }
    }
}