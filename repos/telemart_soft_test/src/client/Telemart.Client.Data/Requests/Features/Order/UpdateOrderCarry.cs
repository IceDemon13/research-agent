using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderCarry : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderCarryDto>
    {
        public UpdateOrderCarry(int orderId, UpdateOrderCarryDto dto)
            : base(orderId, dto, ApiResources.Orders, "set_carry")
        {
        }
    }
}