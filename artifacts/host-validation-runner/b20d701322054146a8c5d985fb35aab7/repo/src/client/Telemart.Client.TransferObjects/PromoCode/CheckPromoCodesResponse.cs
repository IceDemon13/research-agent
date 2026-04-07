using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class CheckPromoCodesResponse
    {
        [JsonProperty("promo_codes")]
        public List<CheckPromoCodeResult> PromoCodes { get; set; }

        [JsonProperty("products")]
        public List<CheckPromoCodeProductResponse> Products { get; set; }
    }
}