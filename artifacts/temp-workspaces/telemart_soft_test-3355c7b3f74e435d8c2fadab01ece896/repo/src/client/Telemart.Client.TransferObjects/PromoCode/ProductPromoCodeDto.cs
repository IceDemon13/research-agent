using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public sealed class ProductPromoCodeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("promo_code")]
        public string PromoCode { get; init; }

        [JsonProperty("show_in_site")]
        public bool ShowInSite { get; init; }

        [JsonProperty("show_in_current_product")]
        public bool ShowInCurrentProduct { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("price")]
        public double? Price { get; init; }
    }
}
