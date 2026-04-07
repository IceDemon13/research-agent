using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductLeftoversDto
    {
        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("warehouse_name")]
        public string WarehouseName { get; set; }

        [JsonProperty("warehouse_position")]
        public int WarehousePosition { get; set; }

        [JsonProperty("warehouse_items")]
        public int WarehouseItems { get; set; }

        [JsonProperty("reserved_by_orders")]
        public int ReservedByOrders { get; set; }

        [JsonProperty("reserved_by_return_invoices")]
        public int ReservedByReturnInvoices { get; set; }

        [JsonProperty("reserved_by_assembly_complectation")]
        public int ReservedByAssemblyComplectation { get; set; }

        [JsonProperty("reserved_by_movement")]
        public int ReservedByMovement { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }
    }
}