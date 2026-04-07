using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoHotlineCompetitorPriceDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("abc_id")]
        public int AbcId { get; set; }

        [JsonProperty("ratio")]
        public double? Ratio { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }
    }
}