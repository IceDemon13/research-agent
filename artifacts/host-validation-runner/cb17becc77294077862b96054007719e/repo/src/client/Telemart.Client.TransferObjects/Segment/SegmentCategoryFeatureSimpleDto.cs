using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class SegmentCategoryFeatureSimpleDto
    {
        public SegmentCategoryFeatureSimpleDto(int segmentCategorySettingsId, int featureValueId)
        {
            SegmentCategorySettingsId = segmentCategorySettingsId;
            FeatureValueId = featureValueId;
        }

        [JsonProperty("segment_category_settings_id")]
        public int SegmentCategorySettingsId { get; set; }

        [JsonProperty("feature_value_id")]
        public int FeatureValueId { get; set; }
    }
}