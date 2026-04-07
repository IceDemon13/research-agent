using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.LogisticsAnalitics
{
    public sealed record LogisticsAnaliticsInvoiceDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

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