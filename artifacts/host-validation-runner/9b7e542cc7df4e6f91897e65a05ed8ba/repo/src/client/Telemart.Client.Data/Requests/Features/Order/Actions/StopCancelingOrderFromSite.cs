using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class StopCancelingOrderFromSite : CallEntityActionRequestResultBase<OrderDto>
    {
        public StopCancelingOrderFromSite(int orderId)
            : base(orderId, ApiResources.Orders, "stop_cancel")
        {
        }
    }
}