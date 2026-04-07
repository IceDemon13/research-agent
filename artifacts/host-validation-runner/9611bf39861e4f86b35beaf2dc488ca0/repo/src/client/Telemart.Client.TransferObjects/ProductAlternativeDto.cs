using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductAlternativeDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("alternative_type")]
        public int AlternativeType { get; set; }

        [JsonProperty("warehouse_quantity_free")]
        public int? WarehouseQuantityFree { get; set; }
    }
}