using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AccessoryCategoryFeatureSaveDto
    {
        public AccessoryCategoryFeatureSaveDto(int id, int accessoryCategoryId, int featureId, int featureValueId)
        {
            AccessoryCategoryId = accessoryCategoryId;
            FeatureId = featureId;
            FeatureValueId = featureValueId;
            Id = id;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("accessory_category_id")]
        public int AccessoryCategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("feature_value_id")]
        public int FeatureValueId { get; set; }
    }
}