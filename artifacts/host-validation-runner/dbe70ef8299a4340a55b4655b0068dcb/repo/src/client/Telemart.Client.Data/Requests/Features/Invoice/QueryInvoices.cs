using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Invoice
{
    public sealed class QueryInvoices : QueryEntitiesPagedRequestBase<InvoiceDto>
    {
        public QueryInvoices(IFilteringItem filter)
            : base(filter, ApiResources.Invoices)
        {
        }
    }
}