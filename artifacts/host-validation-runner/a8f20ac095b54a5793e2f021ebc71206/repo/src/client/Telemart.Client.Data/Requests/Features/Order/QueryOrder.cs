using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryOrder : QueryEntityRequestBase<OrderDto>
    {
        public QueryOrder(int orderId)
            : base(ApiResources.Orders, orderId)
        {
        }
    }
}