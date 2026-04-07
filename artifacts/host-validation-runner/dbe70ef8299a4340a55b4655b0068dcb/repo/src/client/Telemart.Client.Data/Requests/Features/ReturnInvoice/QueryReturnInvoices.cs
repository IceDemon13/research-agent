using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.ReturnInvoice
{
    public sealed class QueryReturnInvoices : QueryEntitiesPagedRequestBase<ReturnInvoiceDto>
    {
        public QueryReturnInvoices(IFilteringItem filter)
            : base(filter, ApiResources.ReturnInvoices)
        {
        }
    }
}
