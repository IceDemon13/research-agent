using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice.Actions
{
    public sealed class SetInvoiceReturnedProducts : CallEntityActionRequestResultBase<InvoiceDto>
    {
        public SetInvoiceReturnedProducts(int id, int? warehouseId = null)
            : base(id, ApiResources.Invoices, $"set_returned_products/{warehouseId}")
        {
        }
    }
}