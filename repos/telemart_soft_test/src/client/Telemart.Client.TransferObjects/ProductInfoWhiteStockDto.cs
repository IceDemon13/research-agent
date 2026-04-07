using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductInfoWhiteStockDto
    {
        [JsonProperty("organization")]
        public string OrganizationName { get; set; }

        [JsonProperty("price_uah")]
        public decimal PriceUah { get; set; }

        [JsonProperty("price_usd")]
        public decimal PriceUsd { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("quantity_free")]
        public int QuantityFree { get; set; }
    }
}