using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductWarehouseDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("quantity_free")]
        public int QuantityFree { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}