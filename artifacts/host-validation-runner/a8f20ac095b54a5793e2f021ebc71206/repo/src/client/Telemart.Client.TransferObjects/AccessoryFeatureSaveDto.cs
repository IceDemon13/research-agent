using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AccessoryFeatureSaveDto
    {
        public AccessoryFeatureSaveDto(int id, int featureId, int featureValueId)
        {
            Id = id;
            FeatureId = featureId;
            FeatureValueId = featureValueId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("feature_value_id")]
        public int FeatureValueId { get; set; }
    }
}