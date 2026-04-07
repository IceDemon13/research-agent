using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderPayment : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderPaymentDto>
    {
        public UpdateOrderPayment(int orderId, UpdateOrderPaymentDto dto)
            : base(orderId, dto, ApiResources.Orders, "set_payment")
        {
        }
    }
}