using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssembledComputerRuleCategoryUpdateDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("use_any_products")]
        public bool UseAnyProducts { get; set; }

        [JsonProperty("compare_method")]
        public string CompareMethod { get; set; }

        [JsonProperty("product_type_ids")]
        public int[] ProductTypeIds { get; init; }
    }
}