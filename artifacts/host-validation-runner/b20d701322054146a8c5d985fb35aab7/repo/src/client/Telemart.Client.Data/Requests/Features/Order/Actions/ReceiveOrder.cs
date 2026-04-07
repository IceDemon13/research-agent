using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class ReceiveOrder : CallEntityActionRequestResultBase<OrderDto>
    {
        public ReceiveOrder(int orderId)
            : base(orderId, ApiResources.Orders, "receive")
        {
        }
    }
}