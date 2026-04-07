using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryOrderGiveInfo : QueryEntityRequestBase<Result<OrderGiveInfoDto>>
    {
        public QueryOrderGiveInfo(int orderId)
            : base(ApiResources.Orders, orderId, "give")
        {
        }
    }
}