using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class ExpireOrder : CallEntityActionWithBodyRequestResultBase<OrderDto, ExpireOrder.OrderExpireDto>
    {
        public ExpireOrder(int orderId, int warehouseId, string trackNumber, string comment)
            : base(orderId, new OrderExpireDto(orderId, warehouseId, trackNumber, comment), ApiResources.Orders, "expire")
        {
        }

        public class OrderExpireDto
        {
            public OrderExpireDto(int id, int warehouseId, string trackNumber, string comment)
            {
                Id = id;
                WarehouseId = warehouseId;
                TrackNumber = trackNumber;
                Comment = comment;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("warehouse_id")]
            public int WarehouseId { get; set; }

            [JsonProperty("track_number")]
            public string TrackNumber { get; set; }

            [JsonProperty("comment")]
            public string Comment { get; set; }
        }
    }
}