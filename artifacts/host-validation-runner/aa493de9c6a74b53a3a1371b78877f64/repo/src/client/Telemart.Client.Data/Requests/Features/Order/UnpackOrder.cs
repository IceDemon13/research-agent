using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UnpackOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, UnpackOrder.UnpackOrderDto>
    {
        public UnpackOrder(int orderId, int reasonId, string comment, int[] cellIds)
            : base(orderId, new UnpackOrderDto(orderId, reasonId, comment, cellIds), ApiResources.Orders, "unpack")
        {
        }

        public class UnpackOrderDto
        {
            public UnpackOrderDto(int orderId, int reasonId, string comment, int[] cellIds)
            {
                OrderId = orderId;
                ReasonId = reasonId;
                Comment = comment;
                CellIds = cellIds;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; init; }

            [JsonProperty("unpack_reason_id")]
            public int ReasonId { get; init; }

            [JsonProperty("comment")]
            public string Comment { get; init; }

            [JsonProperty("cell_ids")]
            public int[] CellIds { get; init; }
        }
    }
}
