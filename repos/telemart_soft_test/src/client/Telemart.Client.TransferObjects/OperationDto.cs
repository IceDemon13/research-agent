using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OperationDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}