using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class PromoCodeSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("sell_plan")]
        public int? SellPlan { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("date_start")]
        public DateTime DateStart { get; set; }

        [JsonProperty("date_end")]
        public DateTime DateEnd { get; set; }

        [JsonProperty("meta_title")]
        public string MetaTitle { get; set; }

        [JsonProperty("meta_title_ukr")]
        public string MetaTitleUkr { get; set; }

        [JsonProperty("meta_title_en")]
        public string MetaTitleEn { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("description_ukr")]
        public string DescriptionUkr { get; set; }

        [JsonProperty("description_en")]
        public string DescriptionEn { get; set; }

        [JsonProperty("show_in_site")]
        public bool ShowInSite { get; set; }

        [JsonProperty("products")]
        public PromoCodeProductDto[] Products { get; set; }

        [JsonProperty("bundle_categories")]
        public PromoCodeBundleCategoryDto[] BundleCategories { get; set; }

        [JsonProperty("bundle_products")]
        public PromoCodeBundleProductDto[] BundleProducts { get; set; }
    }
}