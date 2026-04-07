using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderProductDeliveryCostDto
    {
        public OrderProductDeliveryCostDto(
            int productId,
            int quantity,
            decimal price,
            int currencyId,
            bool freeDelivery)
        {
            ProductId = productId;
            Quantity = quantity;
            Price = price;
            CurrencyId = currencyId;
            FreeDelivery = freeDelivery;
        }

        [JsonProperty("currecy_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }
    }
}