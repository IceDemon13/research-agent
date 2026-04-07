using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderProductState : UpdateEntityResultRequestBase<OrderDto, OrderProductChangeStateDto>
    {
        public UpdateOrderProductState(int orderId, int orderProductId, int orderProductStateId)
            : base(new OrderProductChangeStateDto(orderProductId, orderProductStateId), ApiResources.Orders, orderId, "products", orderProductId, "state")
        {
        }
    }
}