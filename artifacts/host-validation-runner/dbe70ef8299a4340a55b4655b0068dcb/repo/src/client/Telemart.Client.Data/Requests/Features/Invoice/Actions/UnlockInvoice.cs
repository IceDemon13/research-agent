using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class UnlockInvoice : UnlockRequestBase<InvoiceDto>
    {
        public UnlockInvoice(int id, bool force = false)
            : base(force, ApiResources.Invoices, id)
        {
        }
    }
}