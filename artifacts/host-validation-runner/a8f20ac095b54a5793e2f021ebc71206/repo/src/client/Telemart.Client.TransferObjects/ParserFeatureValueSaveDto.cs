using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserFeatureValueSaveDto
    {
        public ParserFeatureValueSaveDto(int id, int? featureValueId, int stateId)
        {
            Id = id;
            FeatureValueId = featureValueId;
            StateId = stateId;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }
    }
}