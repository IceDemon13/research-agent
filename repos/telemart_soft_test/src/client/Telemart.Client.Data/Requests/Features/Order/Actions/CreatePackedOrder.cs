using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class CreatePackedOrder : CallActionWithBodyRequestResultBase<OrderDto, OrderCreatePackedDto>
    {
        public CreatePackedOrder(OrderCreatePackedDto dto)
            : base(dto, ApiResources.Orders, "create_packed")
        {
        }
    }
}
