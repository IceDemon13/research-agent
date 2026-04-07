using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class SegmentCategoryFeatureDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("segment_id")]
        public int SegmentId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("segment_category_settings_id")]
        public int SegmentCategorySettingsId { get; set; }

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