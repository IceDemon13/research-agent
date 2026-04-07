using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderWarranty : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderWarrantyDto>
    {
        public UpdateOrderWarranty(int id, UpdateOrderWarrantyDto dto)
            : base(id, dto, ApiResources.Orders, "set_warranty")
        {
        }
    }
}