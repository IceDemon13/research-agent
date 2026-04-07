using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class TryOpenInvoiceForEditing : UpdateEntityRequestBase<SuccessResponse, object>
    {
        public TryOpenInvoiceForEditing(int invoiceId)
            : base(null, ApiResources.Invoices, invoiceId, "edit")
        {
        }
    }
}