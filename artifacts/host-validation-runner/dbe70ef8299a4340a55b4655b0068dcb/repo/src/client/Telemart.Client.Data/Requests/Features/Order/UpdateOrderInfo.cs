using System;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderInfo : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderInfoDto>
    {
        public UpdateOrderInfo(int orderId, DateTime? receiveTime, string comment)
            : base(orderId, new UpdateOrderInfoDto(orderId, receiveTime, comment), ApiResources.Orders, "set_info")
        {
        }
    }
}