using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class TradeInSegmentCategoryFeatureSimpleDto
    {
        public TradeInSegmentCategoryFeatureSimpleDto(int tradeInSegmentCategorySettingsId, int featureValueId)
        {
            TradeInSegmentCategorySettingsId = tradeInSegmentCategorySettingsId;
            FeatureValueId = featureValueId;
        }

        [JsonProperty("trade_in_segment_category_settings_id")]
        public int TradeInSegmentCategorySettingsId { get; init; }

        [JsonProperty("feature_value_id")]
        public int FeatureValueId { get; init; }
    }
}