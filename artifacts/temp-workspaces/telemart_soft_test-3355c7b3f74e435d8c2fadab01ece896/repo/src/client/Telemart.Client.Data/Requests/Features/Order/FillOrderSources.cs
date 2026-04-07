using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class FillOrderSources : CallEntityActionWithBodyRequestResultBase<OrderDto, OrderFillSourcesDto>
    {
        public FillOrderSources(int orderId, int[] warehousesToUse, bool useInvoices, bool useMovements)
            : base(orderId, new OrderFillSourcesDto(orderId, warehousesToUse, useInvoices, useMovements), ApiResources.Orders, "fill_sources")
        {
        }
    }
}