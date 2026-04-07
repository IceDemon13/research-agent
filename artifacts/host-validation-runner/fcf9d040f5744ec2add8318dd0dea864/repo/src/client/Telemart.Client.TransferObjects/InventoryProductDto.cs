using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InventoryProductDto
    {
        [JsonProperty("id_inventory_product")]
        public int Id { get; set; }

        [JsonProperty("id_inventory")]
        public int InventoryId { get; set; }

        [JsonProperty("id_product")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("quantity_real")]
        public int QuantityReal { get; set; }

        [JsonProperty("quantity_free")]
        public int QuantityFree { get; set; }
    }
}