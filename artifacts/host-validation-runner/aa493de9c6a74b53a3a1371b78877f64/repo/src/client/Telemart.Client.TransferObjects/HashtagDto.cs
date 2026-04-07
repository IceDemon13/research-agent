using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class HashtagDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}