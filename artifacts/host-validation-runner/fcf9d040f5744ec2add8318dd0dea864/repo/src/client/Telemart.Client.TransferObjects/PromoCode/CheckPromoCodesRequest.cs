using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class CheckPromoCodesRequest
    {
        public CheckPromoCodesRequest(int[] promoIds, CheckPromoCodeProductRequest[] products, DateTime? dateTime)
        {
            PromoCodeIds = promoIds;
            Products = products;
            DateTime = dateTime;
            CheckBundleWarehouseAvail = true;
        }

        public CheckPromoCodesRequest(string[] promos, CheckPromoCodeProductRequest[] products, DateTime? dateTime)
        {
            PromoCodes = promos;
            Products = products;
            DateTime = dateTime;
            CheckBundleWarehouseAvail = true;
        }

        [JsonProperty("promo_codes")]
        public string[] PromoCodes { get; init; }

        [JsonProperty("promo_code_ids")]
        public int[] PromoCodeIds { get; init; }

        [JsonProperty("products")]
        public CheckPromoCodeProductRequest[] Products { get; init; }

        [JsonProperty("date_time")]
        public DateTime? DateTime { get; init; }

        [JsonProperty("check_bundle_warehouse_avail")]
        public bool CheckBundleWarehouseAvail { get; init; }
    }
}