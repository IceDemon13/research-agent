using Newtonsoft.Json;
using System.Collections.Generic;

namespace Telemart.Client.TransferObjects.PackList
{
    public class PackListPrintProductDto
    {
        [JsonProperty("sequence_position")]
        public int? SequencePosition { get; init; }

        [JsonProperty("product_id")]
        public int? ProductId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("quantity")]
        public int? Quantity { get; init; }

        [JsonProperty("barcode")]
        public string Barcode { get; init; }

        [JsonProperty("order_id")]
        public int? OrderId { get; init; }

        [JsonProperty("warehouse_quantity")]
        public int? WarehouseQuantity { get; init; }

        [JsonProperty("warehouse_quantity_free")]
        public int? WarehouseQuantityFree { get; init; }

        [JsonProperty("child_products")]
        public IEnumerable<PackListPrintProductDto> ChildProducts { get; init; }
    }
}