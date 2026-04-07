using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserFeatureSaveDto
    {
        public ParserFeatureSaveDto(int id, int? featureId, int stateId)
        {
            Id = id;
            FeatureId = featureId;
            StateId = stateId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }
    }
}