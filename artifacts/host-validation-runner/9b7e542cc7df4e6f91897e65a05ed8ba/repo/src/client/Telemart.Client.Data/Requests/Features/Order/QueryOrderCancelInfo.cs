using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryOrderCancelInfo : QueryEntityRequestBase<Result<OrderCancelInfoDto>>
    {
        public QueryOrderCancelInfo(int orderId)
            : base(ApiResources.Orders, orderId, "cancel")
        {
        }
    }
}