using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OwnershipFormDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }
    }
}