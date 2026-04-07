using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrder : UpdateEntityResultRequestBase<OrderDto, OrderSaveDto>
    {
        public UpdateOrder(int orderId, OrderSaveDto dto)
            : base(dto, ApiResources.Orders, orderId)
        {
        }
    }
}
