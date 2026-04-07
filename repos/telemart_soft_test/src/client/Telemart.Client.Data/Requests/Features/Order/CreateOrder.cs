using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CreateOrder : CreateEntityResultRequestBase<OrderDto, OrderCreateDto>
    {
        public CreateOrder(OrderCreateDto dto)
            : base(dto, ApiResources.Orders)
        {
        }
    }
}
