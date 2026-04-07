using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.GuestProduct.Actions
{
    public sealed class ReceiveGuestProduct : CallActionWithBodyRequestResultBase<OrderDto, ReceiveGuestProductDto>
    {
        public ReceiveGuestProduct(ReceiveGuestProductDto dto)
            : base(dto, ApiResources.GuestProducts, "receive")
        {
        }
    }
}