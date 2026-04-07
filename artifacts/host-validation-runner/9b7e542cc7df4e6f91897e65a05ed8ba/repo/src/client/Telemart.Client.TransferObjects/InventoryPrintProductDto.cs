using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class InventoryPrintProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("parent_category_name")]
        public string ParentCategoryName { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("quantity_free")]
        public int QuantityFree { get; set; }
    }
}