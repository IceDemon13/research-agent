using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryOrderPackInfo : QueryEntityRequestBase<Result<OrderPackInfoDto>>
    {
        public QueryOrderPackInfo(int orderId)
            : base(ApiResources.Orders, orderId, "pack")
        {
        }
    }
}