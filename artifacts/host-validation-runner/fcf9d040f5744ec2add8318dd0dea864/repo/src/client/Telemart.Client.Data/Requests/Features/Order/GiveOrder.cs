using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class GiveOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, GiveOrder.OrderGiveDto>
    {
        public GiveOrder(int orderId, int[] cellIds)
            : base(orderId, new OrderGiveDto(orderId, cellIds), ApiResources.Orders, "give")
        {
        }

        public class OrderGiveDto
        {
            public OrderGiveDto(int orderId, int[] cellIds)
            {
                OrderId = orderId;
                CellIds = cellIds;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("cell_ids")]
            public int[] CellIds { get; set; }
        }
    }
}
