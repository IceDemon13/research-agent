using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public sealed class FeaturePositionDto
    {
        public FeaturePositionDto(int featureId, int position)
        {
            FeatureId = featureId;
            Position = position;
        }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}