using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoContractorSalesHistoryDto
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonProperty("date_time")]
        public DateTime DateTime { get; set; }
    }
}