using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class OrderSplitProduct : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderSplitProductDto>
    {
        public OrderSplitProduct(int orderId, int orderProductId, int quantity)
            : base(orderId, new OrderSplitProductDto(orderId, orderProductId, quantity), ApiResources.Orders, "split")
        {
        }
    }
}