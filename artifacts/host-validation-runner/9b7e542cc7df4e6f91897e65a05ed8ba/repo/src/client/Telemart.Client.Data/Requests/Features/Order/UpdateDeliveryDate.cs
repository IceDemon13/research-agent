using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateDeliveryDate : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderDeliveryDateDto>
    {
        public UpdateDeliveryDate(int orderId, DateTime? deliveryDateFrom, DateTime? deliveryDateTo, int? orderStateChangeReasonId)
            : base(
                orderId,
                new UpdateOrderDeliveryDateDto
                {
                    DeliveryDateFrom = deliveryDateFrom,
                    DeliveryDateTo = deliveryDateTo,
                    OrderStateChangeReasonId = orderStateChangeReasonId
                },
                ApiResources.Orders,
                "set_delivery_date")
        {
        }
    }
}