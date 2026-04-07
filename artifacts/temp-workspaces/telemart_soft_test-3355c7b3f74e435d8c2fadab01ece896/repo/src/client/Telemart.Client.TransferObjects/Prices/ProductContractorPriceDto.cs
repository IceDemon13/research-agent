using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Prices
{
    public class ProductContractorPriceDto
    {
        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; init; }

        [JsonProperty("allow_documents")]
        public bool AllowDocuments { get; init; }

        [JsonProperty("abc_id")]
        public int AbcId { get; init; }

        [JsonProperty("avail_type_id")]
        public int AvailTypeId { get; init; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }
    }
}