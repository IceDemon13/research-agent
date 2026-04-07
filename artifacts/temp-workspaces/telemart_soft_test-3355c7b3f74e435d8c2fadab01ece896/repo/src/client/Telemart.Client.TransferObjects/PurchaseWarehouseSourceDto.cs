using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PurchaseWarehouseSourceDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("warehouse_name")]
        public string WarehouseName { get; set; }

        [JsonProperty("warehouse_position")]
        public int WarehousePosition { get; set; }

        [JsonProperty("warehouse_items")]
        public int WarehouseItems { get; set; }

        [JsonProperty("reserved_quantity")]
        public int ReservedQuantity { get; set; }

        [JsonProperty("target_warehouse_id")]
        public int? TargetWarehouseId { get; set; }

        [JsonProperty("date_delivery")]
        public DateTime? DeliveryDateTime { get; set; }
    }
}