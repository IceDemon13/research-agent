using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ParserAliasDto
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("pn")]
        public string PartNumber { get; set; }

        [JsonProperty("creaed_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("modified_on")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int? ModifiedById { get; set; }

        [JsonProperty("postponed_to")]
        public DateTime? PostponedTo { get; set; }

        [JsonProperty("category_ids")]
        public IReadOnlyCollection<int> CategoryIds { get; set; }

        [JsonProperty("contractor_products")]
        public IReadOnlyCollection<ParserContractorProductDto> ContractorProducts { get; set; }
    }
}