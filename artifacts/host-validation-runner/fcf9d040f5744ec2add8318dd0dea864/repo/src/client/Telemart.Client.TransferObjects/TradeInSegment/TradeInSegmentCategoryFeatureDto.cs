using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class TradeInSegmentCategoryFeatureDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("trade_in_segment_id")]
        public int TradeInSegmentId { get; init; }

        [JsonProperty("trade_in_segment_category_settings_id")]
        public int TradeInSegmentCategorySettingsId { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; init; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; init; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; init; }

        [JsonProperty("feature_value_name")]
        public string FeatureValueName { get; init; }
    }
}