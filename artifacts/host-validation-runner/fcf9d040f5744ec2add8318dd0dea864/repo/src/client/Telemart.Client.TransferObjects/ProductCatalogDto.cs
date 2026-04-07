using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductCatalogDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("based_on")]
        public int? BasedOn { get; init; }

        [JsonProperty("manufactor")]
        public string Manufactor { get; init; }

        [JsonProperty("prefix_rus")]
        public string PrefixRus { get; init; }

        [JsonProperty("prefix_ukr")]
        public string PrefixUkr { get; init; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; init; }

        [JsonProperty("name_en")]
        public string NameEn { get; init; }

        [JsonProperty("model")]
        public string Model { get; init; }

        [JsonProperty("model_ukr")]
        public string ModelUkr { get; init; }

        [JsonProperty("model_en")]
        public string ModelEn { get; init; }

        [JsonProperty("modific")]
        public string Modific { get; init; }

        [JsonProperty("color")]
        public string Color { get; init; }

        [JsonProperty("color_primary_id")]
        public int? ColorPrimaryId { get; init; }

        [JsonProperty("color_secondary_id")]
        public int? ColorSecondaryId { get; init; }

        [JsonProperty("part_number")]
        public string PartNumber { get; init; }

        [JsonProperty("keywords")]
        public string Keywords { get; init; }

        [JsonProperty("yandex_id")]
        public string YandexId { get; init; }

        [JsonProperty("warranty_retail_id")]
        public int WarrantyRetailId { get; init; }

        [JsonProperty("warranty_wholesale_id")]
        public int WarrantyWholesaleId { get; init; }

        [JsonProperty("warranty_type_id")]
        public int? WarrantyTypeId { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("is_no")]
        public bool IsNo { get; init; }

        [JsonProperty("active")]
        public double Active { get; init; }

        [JsonProperty("activated_on")]
        public DateTime? ActivatedOn { get; init; }

        [JsonProperty("group_name")]
        public string GroupName { get; init; }

        [JsonProperty("group_feature_id")]
        public int? GroupFeatureId { get; init; }

        [JsonProperty("assembled_computer_rule_base_product_id")]
        public int? AssembledComputerRuleBaseProductId { get; init; }
    }
}