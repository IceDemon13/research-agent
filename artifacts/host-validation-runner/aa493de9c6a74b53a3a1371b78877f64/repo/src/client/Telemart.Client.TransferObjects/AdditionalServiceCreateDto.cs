using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServiceCreateDto
    {
        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("description_ua")]
        public string DescriptionUa { get; set; }

        [JsonProperty("description_en")]
        public string DescriptionEn { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("min_price")]
        public decimal MinPrice { get; set; }

        [JsonProperty("percent")]
        public decimal Percent { get; set; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; set; }

        [JsonProperty("priority_type_id")]
        public int PriorityTypeId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("additional_warranty")]
        public bool AdditionalWarranty { get; set; }

        [JsonProperty("auto_add")]
        public bool AutoAdd { get; set; }

        [JsonProperty("control_in_movements")]
        public bool ControlInMovements { get; set; }

        [JsonProperty("presence_of_customer")]
        public bool PresenceOfCustomer { get; set; }

        [JsonProperty("is_local")]
        public bool IsLocal { get; set; }

        [JsonProperty("require_products_to_provide")]
        public bool RequireProductsToProvide { get; set; }

        [JsonProperty("assembly_part")]
        public bool AssemblyPart { get; set; }

        [JsonProperty("create_discount")]
        public bool CreateDiscount { get; set; }

        [JsonProperty("disassembly")]
        public bool Disassembly { get; set; }

        [JsonProperty("price_ids")]
        public int[] PriceIds { get; set; }

        [JsonProperty("slave_categories")]
        public List<AdditionalServiceSlaveCategoryCreateDto> SlaveCategories { get; set; }

        [JsonProperty("provide_rules")]
        public List<AdditionalServiceProvideRuleCreateDto> ProvideRules { get; set; }
    }
}