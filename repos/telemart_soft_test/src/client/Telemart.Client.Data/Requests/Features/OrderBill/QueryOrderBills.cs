using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.OrderBill
{
    public sealed class QueryOrderBills : QueryEntitiesRequestBase<OrderBillDto>
    {
        public QueryOrderBills(QueryOrderBillsFilteringItem filter)
            : base(filter, ApiResources.OrderBills)
        {
        }

        public class QueryOrderBillsFilteringItem : FilteringItemBase
        {
            public QueryOrderBillsFilteringItem(int orderId)
            {
                OrderId = orderId;
            }

            [FilteringItemProperty("order_id")]
            public int OrderId { get; set; }
        }
    }
}
