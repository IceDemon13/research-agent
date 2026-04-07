using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ShowcaseCategorySaveDto
    {
        public ShowcaseCategorySaveDto(int id, int warehouseId, int categoryId, int quantity, string placeName)
        {
            Id = id;
            WarehouseId = warehouseId;
            CategoryId = categoryId;
            Quantity = quantity;
            PlaceName = placeName;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("place_name")]
        public string PlaceName { get; set; }
    }
}