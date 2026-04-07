using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AccessoryCategoryFeatureDto
    {
        [JsonProperty("accessory_category_id")]
        public int AccessoryCategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("feature_value_id")]
        public int FeatureValueId { get; set; }

        [JsonProperty("feature_value_name")]
        public string FeatureValueName { get; set; }
    }
}