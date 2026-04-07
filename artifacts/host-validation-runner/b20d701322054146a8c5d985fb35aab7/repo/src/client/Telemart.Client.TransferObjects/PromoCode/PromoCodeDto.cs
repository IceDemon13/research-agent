using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PromoCode
{
    public class PromoCodeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("sell_plan")]
        public int? SellPlan { get; init; }

        [JsonProperty("value")]
        public string Value { get; init; }

        [JsonProperty("date_start")]
        public DateTime DateStart { get; init; }

        [JsonProperty("date_end")]
        public DateTime DateEnd { get; init; }

        [JsonProperty("meta_title")]
        public string MetaTitle { get; init; }

        [JsonProperty("meta_title_ukr")]
        public string MetaTitleUkr { get; init; }

        [JsonProperty("meta_title_en")]
        public string MetaTitleEn { get; init; }

        [JsonProperty("show_in_site")]
        public bool ShowInSite { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }
    }
}