using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceProvideRuleCreateDto
    {
        public AdditionalServiceProvideRuleCreateDto(
            int? categoryId,
            int? productId,
            int? featureId,
            int? featureValueId)
        {
            CategoryId = categoryId;
            ProductId = productId;
            FeatureId = featureId;
            FeatureValueId = featureValueId;
        }

        [JsonProperty("category_id")]
        public int? CategoryId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; set; }
    }
}