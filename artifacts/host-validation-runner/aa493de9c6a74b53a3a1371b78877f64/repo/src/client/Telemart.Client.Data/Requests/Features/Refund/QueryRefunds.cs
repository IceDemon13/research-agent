using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Refund
{
    public sealed class QueryRefunds : QueryEntitiesPagedRequestBase<RefundDto>
    {
        public QueryRefunds(IFilteringItem filter)
            : base(filter, ApiResources.Refunds)
        {
        }
    }
}