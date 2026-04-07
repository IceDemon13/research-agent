using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CancelOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderCancelDto>
    {
        public CancelOrder(int id, int changeReasonId, string comment, int[] cellIds)
            : base(id, new OrderCancelDto(id, changeReasonId, comment, cellIds), ApiResources.Orders, "cancel")
        {
        }
    }
}