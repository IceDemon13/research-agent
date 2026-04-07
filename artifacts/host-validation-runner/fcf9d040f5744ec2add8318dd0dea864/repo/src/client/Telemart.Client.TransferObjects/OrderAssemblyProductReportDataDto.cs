using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderAssemblyProductReportDataDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("warehouse_quantity")]
        public int WarehouseQuantity { get; set; }

        [JsonProperty("warehouse_quantity_free")]
        public int WarehouseQuantityFree { get; set; }

        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("order_product_id")]
        public int? OrderProductId { get; init; }

        [JsonIgnore]
        public int Left { get; set; }
    }
}