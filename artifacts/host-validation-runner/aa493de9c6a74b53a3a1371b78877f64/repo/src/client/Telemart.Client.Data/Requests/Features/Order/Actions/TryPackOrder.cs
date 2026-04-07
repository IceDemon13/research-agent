using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class TryPackOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderPackDto>
    {
        public TryPackOrder(OrderPackDto dto)
            : base(dto.Id, dto, ApiResources.Orders, "pack")
        {
        }
    }
}