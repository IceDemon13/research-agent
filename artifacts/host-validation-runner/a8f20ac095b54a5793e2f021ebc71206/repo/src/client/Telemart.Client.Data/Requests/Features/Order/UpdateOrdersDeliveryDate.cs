using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrdersDeliveryDate : CallActionWithBodyRequestResultBase<OrderDto[], UpdateOrdersDeliveryDateDto>
    {
        public UpdateOrdersDeliveryDate(UpdateOrdersDeliveryDateDto dto)
            : base(dto, ApiResources.Orders, "orders_set_delivery_date")
        {
        }
    }
}