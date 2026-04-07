using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class FeatureContractorParserSourceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; set; }
    }
}