using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.LogisticsAnalitics
{
    public sealed record LogisticsAnaliticsMovementDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; init; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("date_send")]
        public DateTime DateSend { get; init; }

        [JsonProperty("date_arrive")]
        public DateTime DateArrive { get; init; }

        [JsonProperty("date_receive")]
        public DateTime DateReceive { get; init; }

        [JsonProperty("rows")]
        public int Rows { get; init; }

        [JsonProperty("order_rows")]
        public int OrderRows { get; init; }

        [JsonProperty("orders_count")]
        public int OrdersCount { get; init; }

        [JsonProperty("weight")]
        public double Weight { get; init; }

        [JsonProperty("orders_price")]
        public decimal OrdersPrice { get; init; }
    }
}