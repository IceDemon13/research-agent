using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public class ProductSimpleDto : ILocalіzableEntity
    {
        [JsonProperty("active")]
        public double Active { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("prefix_rus")]
        public string PrefixRus { get; set; }

        [JsonProperty("prefix_ukr")]
        public string PrefixUkr { get; set; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; set; }

        [JsonProperty("name_full_ukr")]
        public string NameFullUkr { get; set; }

        [JsonProperty("name_full_ru")]
        public string NameFullRu { get; set; }

        [JsonProperty("part_number")]
        public string PartNumber { get; init; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("additional_service_id")]
        public int? AdditionalServiceId { get; set; }

        [JsonProperty("additional_service_percent")]
        public decimal? AdditionalServicePercent { get; set; }

        [JsonProperty("additional_service_min_price")]
        public decimal? AdditionalServiceMinPrice { get; set; }

        [JsonProperty("print_warranty_card")]
        public bool PrintWarrantyCard { get; set; }

        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("depth")]
        public int? Depth { get; set; }

        [JsonProperty("parent_link_rewrite")]
        public string ParentLinkRewrite { get; set; }

        [JsonProperty("prices")]
        public IReadOnlyCollection<ProductPriceSimpleDto> Prices { get; set; }

        string ILocalіzableEntity.Name => NameFullRu;

        public string NameUkr => NameFullUkr;

        public string NameEn => string.Empty;
    }
}