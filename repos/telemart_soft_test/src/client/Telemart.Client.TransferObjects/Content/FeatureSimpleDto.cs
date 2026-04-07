using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("group_id")]
        public int GroupId { get; set; }
    }
}