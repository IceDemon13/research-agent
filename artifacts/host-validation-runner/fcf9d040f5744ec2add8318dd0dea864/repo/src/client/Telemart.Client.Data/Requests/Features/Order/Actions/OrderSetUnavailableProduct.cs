using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class OrderSetUnavailableProduct : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderSetUnavailableProductDto>
    {
        public OrderSetUnavailableProduct(int id, OrderSetUnavailableProductDto dto)
            : base(id, dto, ApiResources.Orders, "set_unavailable_product")
        {
        }
    }
}