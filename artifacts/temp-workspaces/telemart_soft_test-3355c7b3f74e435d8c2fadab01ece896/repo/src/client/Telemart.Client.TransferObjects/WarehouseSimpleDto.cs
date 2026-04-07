using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class WarehouseSimpleDto
    {
        [JsonProperty("active")]
        public int Active { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }
}