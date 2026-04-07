using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductTransitDto
    {
        [JsonProperty("available")]
        public int Available { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("date_get")]
        public DateTime DateGet { get; set; }

        [JsonProperty("ignore_transit")]
        public bool IgnoreTransit { get; set; }

        [JsonProperty("overall")]
        public int Overall { get; set; }

        [JsonProperty("suppier_id")]
        public int SupplierId { get; set; }

        [JsonProperty("supplier_warehouse_id")]
        public int? SupplierWarehouseId { get; set; }

        public string TransitName { get; set; }
    }
}