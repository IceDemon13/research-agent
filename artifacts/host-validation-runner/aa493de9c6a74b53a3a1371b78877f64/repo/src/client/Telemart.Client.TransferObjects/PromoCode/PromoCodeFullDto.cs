using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class PromoCodeFullDto : PromoCodeDto
    {
        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("description_ukr")]
        public string DescriptionUkr { get; set; }

        [JsonProperty("description_en")]
        public string DescriptionEn { get; set; }

        [JsonProperty("used_in_orders")]
        public bool UsedInOrders { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<PromoCodeProductDto> Products { get; set; }

        [JsonProperty("bundle_categories")]
        public IReadOnlyCollection<PromoCodeBundleCategoryDto> BundleCategories { get; set; }

        [JsonProperty("bundle_products")]
        public IReadOnlyCollection<PromoCodeBundleProductDto> BundleProducts { get; init; }
    }
}