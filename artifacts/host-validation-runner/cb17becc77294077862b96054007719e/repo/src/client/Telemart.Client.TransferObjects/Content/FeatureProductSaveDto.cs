using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureProductSaveDto
    {
        public FeatureProductSaveDto(int featureId, int featureValueId)
        {
            FeatureValueId = featureValueId;
            FeatureId = featureId;
        }

        [JsonProperty("feature_value_id")]
        public int FeatureValueId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }
    }
}