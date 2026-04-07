using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderSetUnavailableProductDto
    {
        public OrderSetUnavailableProductDto(
            int orderProductId,
            int unavailableProductId,
            decimal unavailableProductPrice,
            int unavailableProductCurrencyId)
        {
            OrderProductId = orderProductId;
            UnavailableProductId = unavailableProductId;
            UnavailableProductPrice = unavailableProductPrice;
            UnavailableProductCurrencyId = unavailableProductCurrencyId;
        }

        [JsonProperty("order_product_id")]
        public int OrderProductId { get; init; }

        [JsonProperty("unavailable_product_id")]
        public int UnavailableProductId { get; init; }

        [JsonProperty("unavailable_product_price")]
        public decimal UnavailableProductPrice { get; init; }

        [JsonProperty("unavailable_product_currency_id")]
        public int UnavailableProductCurrencyId { get; init; }
    }
}