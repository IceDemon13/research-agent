using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductLogisticsResultDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("source_date")]
        public DateTime SourceDate { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("delivery_time")]
        public DateTime DeliveryTime { get; set; }
    }
}