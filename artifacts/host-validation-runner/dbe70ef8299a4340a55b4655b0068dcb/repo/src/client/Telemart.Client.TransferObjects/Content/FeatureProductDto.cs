using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureProductDto
    {
        [JsonProperty("feature_value_id")]
        public int FeatureValueId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }
    }
}