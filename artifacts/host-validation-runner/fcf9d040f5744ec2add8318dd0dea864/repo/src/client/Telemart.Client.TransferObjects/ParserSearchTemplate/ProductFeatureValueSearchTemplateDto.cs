using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSearchTemplate
{
    public sealed class ProductFeatureValueSearchTemplateDto
    {
        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("feature_value")]
        public string FeatureValue { get; set; }

        [JsonProperty("contractor_feature_value")]
        public string ContractorFeatureValue { get; set; }
    }
}