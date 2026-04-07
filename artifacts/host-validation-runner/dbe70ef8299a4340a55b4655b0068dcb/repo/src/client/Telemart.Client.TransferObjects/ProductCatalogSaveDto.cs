using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductCatalogSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("manufactor")]
        public string Manufactor { get; set; }

        [JsonProperty("prefix_rus")]
        public string PrefixRus { get; set; }

        [JsonProperty("prefix_ukr")]
        public string PrefixUkr { get; set; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("model_ukr")]
        public string ModelUkr { get; set; }

        [JsonProperty("model_en")]
        public string ModelEn { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("color_primary_id")]
        public int? ColorPrimaryId { get; set; }

        [JsonProperty("color_secondary_id")]
        public int? ColorSecondaryId { get; set; }

        [JsonProperty("part_number")]
        public string PartNumber { get; set; }

        [JsonProperty("keywords")]
        public string Keywords { get; set; }

        [JsonProperty("yandex_id")]
        public string YandexId { get; set; }

        [JsonProperty("warranty_retail_id")]
        public int WarrantyRetailId { get; set; }

        [JsonProperty("warranty_wholesale_id")]
        public int WarrantyWholesaleId { get; set; }

        [JsonProperty("warranty_type_id")]
        public int? WarrantyTypeId { get; set; }

        [JsonProperty("type_id")]
        public int? TypeId { get; set; }

        [JsonProperty("active_exp")]
        public double ActiveExpected { get; set; }

        [JsonProperty("group_name")]
        public string GroupName { get; set; }

        [JsonProperty("group_feature_id")]
        public int? GroupFeatureId { get; set; }

        [JsonProperty("assembled_computer_rule_base_product_id")]
        public int? AssembledComputerRuleBaseProductId { get; set; }
    }
}