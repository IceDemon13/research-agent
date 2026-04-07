using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserFeatureValueDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_value_id")]
        public int? FeatureValueId { get; protected set; }

        [JsonProperty("feature_value_name")]
        public string FeatureValueName { get; protected set; }

        [JsonProperty("feature_id")]
        public int FeatureId { get; protected set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; protected set; }

        [JsonProperty("state_id")]
        public int StateId { get; protected set; }

        [JsonProperty("parser_feature_id")]
        public int ParserFeatureId { get; protected set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; protected set; }

        [JsonProperty("name")]
        public string Name { get; protected set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; protected set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("category_ids")]
        public int[] CategoryIds { get; set; }

        [JsonProperty("product_ids")]
        public int[] ProductIds { get; set; }
    }
}