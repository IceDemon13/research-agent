using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoMovementDto
    {
        [JsonProperty("available")]
        public int Available { get; set; }

        [JsonProperty("overall")]
        public int Overall { get; set; }

        [JsonProperty("date_in")]
        public DateTime DateIn { get; set; }

        [JsonProperty("from_warehouse_id")]
        public int FromWarehouseId { get; set; }

        [JsonProperty("to_warehouse_id")]
        public int ToWarehouseId { get; set; }
    }
}