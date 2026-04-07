using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class CloseInvoiceEditing : DeleteEntityRequestBase
    {
        public CloseInvoiceEditing(int entityId)
            : base(ApiResources.Invoices, entityId.ToString(), "edit")
        {
        }
    }
}