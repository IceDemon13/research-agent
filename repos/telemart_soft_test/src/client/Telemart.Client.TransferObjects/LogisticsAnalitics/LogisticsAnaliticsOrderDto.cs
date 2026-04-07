using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.LogisticsAnalitics
{
    public sealed record LogisticsAnaliticsOrderDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("delivery_time")]
        public DateTime DeliveryTime { get; init; }

        [JsonProperty("delivery_time_to")]
        public DateTime DeliveryTimeTo { get; init; }

        [JsonProperty("rows")]
        public int Rows { get; init; }

        [JsonProperty("weight")]
        public double Weight { get; init; }

        [JsonProperty("price")]
        public decimal Price { get; init; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; init; }

        [JsonProperty("ready_for_packing")]
        public bool ReadyForPacking { get; init; }

        [JsonProperty("canceled_from_site")]
        public bool CanceledFromSite { get; init; }

        [JsonProperty("event_unpack_and_cancel_order")]
        public bool EventUnpackAndCancelOrder { get; init; }
    }
}