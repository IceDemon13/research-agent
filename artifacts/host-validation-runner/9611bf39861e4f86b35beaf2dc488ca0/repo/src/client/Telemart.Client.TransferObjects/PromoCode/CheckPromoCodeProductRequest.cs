using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class CheckPromoCodeProductRequest
    {
        public CheckPromoCodeProductRequest(int id, int productId, int quantity, decimal price)
        {
            Id = id;
            ProductId = productId;
            Price = price;
            Quantity = quantity;
        }

        public CheckPromoCodeProductRequest()
        {
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("bundle_id")]
        public int? BundleId { get; set; }
    }
}