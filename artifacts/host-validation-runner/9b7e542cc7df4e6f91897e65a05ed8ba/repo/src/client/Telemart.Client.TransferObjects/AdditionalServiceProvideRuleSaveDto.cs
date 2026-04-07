using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceProvideRuleSaveDto
    {
        public AdditionalServiceProvideRuleSaveDto(
            int id,
            int? categoryId,
            int? productId,
            int? featureId,
            int? featureValueId,
            bool active)
        {
            Id = id;
            CategoryId = categoryId;
            ProductId = productId;
            FeatureId = featureId;
            FeatureValueId = featureValueId;
            Active = active;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int? CategoryId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}