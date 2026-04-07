using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceProvideRuleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("additional_service_id")]
        public int AdditionalServiceId { get; set; }

        [JsonProperty("category_id")]
        public int? CategoryId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("feature")]
        public string Feature { get; set; }

        [JsonProperty("feature_value")]
        public string FeatureValue { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}