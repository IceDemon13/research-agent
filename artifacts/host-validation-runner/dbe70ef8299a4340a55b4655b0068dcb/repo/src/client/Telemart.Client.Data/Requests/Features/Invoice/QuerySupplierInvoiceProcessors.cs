using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public class QuerySupplierInvoiceProcessors : QueryEntitiesRequestBase<SupplierInvoiceProcessorDto>
    {
        public QuerySupplierInvoiceProcessors()
            : base($"{ApiResources.Invoices}/supplier_invoice_processors")
        {
        }
    }
}
