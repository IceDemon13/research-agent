using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class MergeOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderMergeDto>
    {
        public MergeOrder(int id, OrderMergeDto dto)
            : base(id, dto, ApiResources.Orders, "merge")
        {
        }
    }
}