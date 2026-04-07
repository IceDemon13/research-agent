using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Bundle
{
    public record BundleProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("discount_mode_id")]
        public int? DiscountModeId { get; init; }

        [JsonProperty("discount_amount")]
        public int? DiscountAmount { get; init; }
    }
}
