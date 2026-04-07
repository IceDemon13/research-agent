using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public class CallTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("parent")]
        public CallTypeDto Parent { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }
    }
}