using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryOrderStateChangeReasons : QueryEntitiesRequestBase<OrderStateChangeReasonDto>
    {
        public QueryOrderStateChangeReasons(bool? withOnlySite)
            : base(new OrderStateChangeReasonsFilter(withOnlySite), $"{ApiResources.Orders}/reasons")
        {
        }

        public class OrderStateChangeReasonsFilter : FilteringItemBase
        {
            public OrderStateChangeReasonsFilter(bool? withOnlySite)
            {
                WithOnlySite = withOnlySite;
            }

            [FilteringItemProperty("with_only_site")]
            public bool? WithOnlySite { get; }
        }
    }
}