using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class ShipOrders : CallActionWithBodyRequestBase<OrdersShippingResult, ShipOrdersDto>
    {
        public ShipOrders(ShipOrdersDto dto)
            : base(dto, ApiResources.Orders, "rt")
        {
        }
    }
}