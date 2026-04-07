using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceSlaveCategorySaveDto
    {
        public AdditionalServiceSlaveCategorySaveDto(
            int id,
            int? categoryId,
            int? productId,
            int? featureId,
            int? featureValueId)
        {
            Id = id;
            CategoryId = categoryId;
            ProductId = productId;
            FeatureId = featureId;
            FeatureValueId = featureValueId;
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
    }
}