using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InventoryProductQuantityRealDto
    {
        [JsonProperty("id_inventory_product")]
        public int? InventoryProductId { get; set; }

        [JsonProperty("id_product")]
        public int ProductId { get; set; }

        [JsonProperty("quantity_real")]
        public int QuantityReal { get; set; }
    }
}