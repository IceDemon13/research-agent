using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class ReconfirmOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderReconfirmDto>
    {
        public ReconfirmOrder(int id, int changeReasonId, string comment, int[] orderProductIds)
            : base(id, new OrderReconfirmDto(id, changeReasonId, comment, orderProductIds), ApiResources.Orders, "reconfirm")
        {
        }
    }
}