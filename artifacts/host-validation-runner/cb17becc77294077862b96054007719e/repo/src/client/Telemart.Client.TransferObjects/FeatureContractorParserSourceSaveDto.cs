using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class FeatureContractorParserSourceSaveDto
    {
        public FeatureContractorParserSourceSaveDto(int id, int priority, int featureId, int contractorId)
        {
            FeatureId = featureId;
            ContractorId = contractorId;
            Priority = priority;
            Id = id;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }
    }
}