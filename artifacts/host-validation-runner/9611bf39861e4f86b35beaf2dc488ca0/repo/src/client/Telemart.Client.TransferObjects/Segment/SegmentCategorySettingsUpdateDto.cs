using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class SegmentCategorySettingsUpdateDto
    {
        public SegmentCategorySettingsUpdateDto(int id, int categoryId, int featureId)
        {
            Id = id;
            CategoryId = categoryId;
            FeatureId = featureId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }
    }
}