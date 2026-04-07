using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class FeatureContractorKeyDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }
    }
}