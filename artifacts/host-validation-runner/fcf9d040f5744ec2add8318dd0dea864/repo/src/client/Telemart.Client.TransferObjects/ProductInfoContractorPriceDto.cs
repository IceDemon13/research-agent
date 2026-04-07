using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoContractorPriceDto
    {
        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("source_price_uah")]
        public decimal SourcePriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonProperty("source_price_usd")]
        public decimal SourcePriceUsd { get; set; }

        [JsonProperty("consider")]
        public bool Consider { get; set; }

        [JsonProperty("contractor_allow_document")]
        public bool ContractorAllowDocument { get; set; }

        [JsonProperty("F2_markup")]
        public double F2Markup { get; set; }

        [JsonProperty("avail")]
        public string Avail { get; set; }

        [JsonProperty("date_add")]
        public DateTime DateAdd { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}