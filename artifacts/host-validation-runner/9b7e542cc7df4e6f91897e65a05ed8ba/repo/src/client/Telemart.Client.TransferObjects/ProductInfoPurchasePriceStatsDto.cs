using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoPurchasePriceStatsDto
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("min_pirce_uah")]
        public decimal MinPriceUah { get; set; }

        [JsonProperty("max_pirce_uah")]
        public decimal MaxPriceUah { get; set; }

        [JsonProperty("min_pirce_usd")]
        public decimal MinPriceUsd { get; set; }

        [JsonProperty("max_pirce_usd")]
        public decimal MaxPriceUsd { get; set; }

        [JsonProperty("average_pirce_usd")]
        public decimal AveragePriceUsd { get; set; }

        [JsonProperty("average_pirce_uah")]
        public decimal AveragePriceUah { get; set; }
    }
}