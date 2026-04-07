using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class YandexMarketCategoryProductsDto
    {
        public YandexMarketCategoryProductsDto(int categoryHid, string[] productIds)
        {
            CategoryHid = categoryHid;
            ProductIds = productIds;
        }

        [JsonProperty("category_hid")]
        public int CategoryHid { get; set; }

        [JsonProperty("product_ids")]
        public string[] ProductIds { get; set; }
    }
}