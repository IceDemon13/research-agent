using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class PromoCodeProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("promo_code_id")]
        public int PromoCodeId { get; set; }

        [JsonProperty("discount_mode_id")]
        public int DiscountModeId { get; set; }

        [JsonProperty("category_id")]
        public int? CategoryId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("amount")]
        public int? Amount { get; set; }

        [JsonProperty("show_in_site")]
        public bool ShowInSite { get; set; }
    }
}