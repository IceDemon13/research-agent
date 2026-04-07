using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class YandexMarketProductFeatureDto
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("group_name")]
        public string GroupName { get; set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("feature_value")]
        public string FeatureValue { get; set; }
    }
}