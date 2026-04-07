using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductPromoCodeSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("promo_code_id")]
        public int PromoCodeId { get; set; }
    }
}