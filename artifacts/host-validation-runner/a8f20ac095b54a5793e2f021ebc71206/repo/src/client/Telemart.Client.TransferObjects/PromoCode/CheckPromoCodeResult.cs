using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class CheckPromoCodeResult
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("promo_code_id")]
        public int? PromoCodeId { get; set; }

        [JsonProperty("promo_code_value")]
        public string PromoCodeValue { get; set; }
    }
}