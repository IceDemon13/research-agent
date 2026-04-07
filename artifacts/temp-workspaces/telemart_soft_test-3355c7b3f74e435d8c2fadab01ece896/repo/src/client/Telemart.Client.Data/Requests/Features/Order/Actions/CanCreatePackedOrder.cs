using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class CanCreatePackedOrder : CallActionWithBodyRequestResultBase<object, OrderCreatePackedDto>
    {
        public CanCreatePackedOrder(OrderCreatePackedDto dto)
            : base(dto, ApiResources.Orders, "can_create_packed")
        {
        }
    }
}
