using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoShowcaseDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_name")]
        public string WarehouseName { get; set; }

        [JsonProperty("capacity")]
        public int Capacity { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}