using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoSearchTemplateDto
    {
        [JsonProperty("search_template_id")]
        public int SearchTemplateId { get; set; }

        [JsonProperty("search_template_name")]
        public string SearchTemplateName { get; set; }

        [JsonProperty("min_price_uah")]
        public decimal MinPriceUah { get; set; }

        [JsonProperty("min_price_usd")]
        public decimal MinPriceUsd { get; set; }

        [JsonProperty("avg_price_uah")]
        public decimal AvgPriceUah { get; set; }

        [JsonProperty("avg_price_usd")]
        public decimal AvgPriceUsd { get; set; }

        [JsonProperty("max_price_uah")]
        public decimal MaxPriceUah { get; set; }

        [JsonProperty("max_price_usd")]
        public decimal MaxPriceUsd { get; set; }
    }
}