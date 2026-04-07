using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class TradeInSegmentCategorySettingsUpdateDto
    {
        public TradeInSegmentCategorySettingsUpdateDto(
            int id,
            int categoryId,
            int featureId)
        {
            Id = id;
            CategoryId = categoryId;
            FeatureId = featureId;
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; init; }
    }
}
