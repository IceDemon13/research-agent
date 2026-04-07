using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoHotlineDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("comment_count")]
        public int CommentCount { get; set; }

        [JsonProperty("offer_count")]
        public int OfferCount { get; set; }

        [JsonProperty("price_min_uah")]
        public decimal PriceMinUah { get; set; }

        [JsonProperty("price_min_usd")]
        public decimal PriceMinUsd { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonIgnore]
        public string DisplayPrice { get; set; }

        [JsonIgnore]
        public string DisplayMinPrice { get; set; }

        [JsonProperty("hotline_competitors")]
        public ProductInfoHotlineCompetitorPriceDto[] HotlineCompetitorPrices { get; set; }
    }
}