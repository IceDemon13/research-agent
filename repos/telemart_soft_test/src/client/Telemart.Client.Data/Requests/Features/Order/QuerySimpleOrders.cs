using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QuerySimpleOrders : QueryEntitiesPagedRequestBase<OrderSimpleDto>
    {
        public QuerySimpleOrders(IFilteringItem filter)
            : base(filter, $"{ApiResources.Orders}/simple")
        {
        }
    }
}