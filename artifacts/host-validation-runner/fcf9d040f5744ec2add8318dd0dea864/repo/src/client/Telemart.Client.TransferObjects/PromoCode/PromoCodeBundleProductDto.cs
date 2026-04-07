using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public record PromoCodeBundleProductDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("product_name")]
        public string ProductName { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("discount_mode_id")]
        public int? DiscountModeId { get; init; }

        [JsonProperty("discount_amount")]
        public int? DiscountAmount { get; init; }
    }
}