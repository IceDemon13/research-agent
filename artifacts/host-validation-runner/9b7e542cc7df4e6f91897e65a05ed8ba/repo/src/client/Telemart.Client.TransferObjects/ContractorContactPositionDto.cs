using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ContractorContactPositionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}