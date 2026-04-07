using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PurchaseMovementSourceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; set; }

        [JsonProperty("warehouse_from_name")]
        public string WarehouseFromName { get; set; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; set; }

        [JsonProperty("warehouse_to_name")]
        public string WarehouseToName { get; set; }

        [JsonProperty("date_out")]
        public DateTime DateOut { get; set; }

        [JsonProperty("date_in")]
        public DateTime DateIn { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("available_quantity")]
        public int AvailableQuantity { get; set; }

        [JsonProperty("date_delivery")]
        public DateTime? DeliveryDateTime { get; set; }
    }
}