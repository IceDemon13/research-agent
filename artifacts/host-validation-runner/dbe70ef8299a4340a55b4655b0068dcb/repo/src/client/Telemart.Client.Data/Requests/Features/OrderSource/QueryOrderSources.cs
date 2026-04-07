using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderSource
{
    public sealed class QueryOrderSources : QueryEntitiesRequestBase<OrderSourceDto>
    {
        public QueryOrderSources()
            : base($"{ApiResources.Orders}/sources")
        {
        }
    }
}