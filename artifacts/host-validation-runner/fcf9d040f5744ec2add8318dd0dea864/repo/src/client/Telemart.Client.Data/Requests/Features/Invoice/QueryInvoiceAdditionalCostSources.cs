using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class QueryInvoiceAdditionalCostSources : QueryEntitiesRequestBase<InvoiceAdditionalCostSourceDto>
    {
        public QueryInvoiceAdditionalCostSources()
            : base($"{ApiResources.Invoices}/{ApiResources.AdditionalCostSources}")
        {
        }
    }
}