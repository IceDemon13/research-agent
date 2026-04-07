using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class CheckPromoCodeProductResponse : CheckPromoCodeProductRequest
    {
        [JsonProperty("price_new")]
        public decimal? PriceNew { get; set; }

        [JsonProperty("bonuses_to_charge")]
        public decimal? BonusesToCharge { get; set; }

        [JsonProperty("promo_code_id")]
        public int? PromoCodeId { get; set; }

        [JsonProperty("promo_code")]
        public string PromoCode { get; set; }

        [JsonProperty("is_gift")]
        public bool IsGift { get; set; }
    }
}