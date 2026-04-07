using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Promo
{
    public class PromoDto : CatalogPromoSimpleDto
    {
        [JsonProperty("product_ids")]
        public int[] ProductIds { get; set; }
    }
}