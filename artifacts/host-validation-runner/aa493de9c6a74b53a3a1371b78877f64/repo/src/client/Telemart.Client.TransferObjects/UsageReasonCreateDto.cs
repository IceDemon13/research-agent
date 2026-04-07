using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UsageReasonCreateDto
    {
        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("entity_id")]
        public int? EntityId { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}