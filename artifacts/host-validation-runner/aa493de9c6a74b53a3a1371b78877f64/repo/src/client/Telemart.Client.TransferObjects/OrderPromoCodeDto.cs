using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record OrderPromoCodeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("promo_code_id")]
        public int PromoCodeId { get; init; }

        [JsonProperty("promo_code_type_id")]
        public int PromoCodeTypeId { get; init; }

        [JsonProperty("promo_code_meta_title")]
        public string PromoCodeMetaTitle { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }
    }
}