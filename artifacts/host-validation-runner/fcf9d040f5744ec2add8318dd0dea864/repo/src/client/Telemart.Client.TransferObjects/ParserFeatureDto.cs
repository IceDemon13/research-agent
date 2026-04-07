using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserFeatureDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("category_ids")]
        public int[] CategoryIds { get; set; }
    }
}