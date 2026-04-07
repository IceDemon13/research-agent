using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class ConfirmOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderConfirmDto>
    {
        public ConfirmOrder(
            int id,
            DateTime deliveryTime,
            DateTime deliveryTimeTo,
            DateTime? dateComplete,
            int packageDeliveryCost,
            int packageDeliveryPaid,
            bool sendSms)
            : base(
                id,
                new OrderConfirmDto(id, deliveryTime, deliveryTimeTo, dateComplete, packageDeliveryCost, packageDeliveryPaid, sendSms),
                ApiResources.Orders,
                "confirm")
        {
        }
    }
}