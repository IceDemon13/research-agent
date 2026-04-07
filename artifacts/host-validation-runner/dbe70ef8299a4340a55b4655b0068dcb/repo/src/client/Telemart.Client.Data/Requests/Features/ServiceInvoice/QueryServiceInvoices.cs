using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceInvoice
{
    public sealed class QueryServiceInvoices : QueryEntitiesPagedRequestBase<ServiceInvoiceDto>
    {
        public QueryServiceInvoices(IFilteringItem filter)
            : base(filter, ApiResources.ServiceInvoices)
        {
        }
    }
}