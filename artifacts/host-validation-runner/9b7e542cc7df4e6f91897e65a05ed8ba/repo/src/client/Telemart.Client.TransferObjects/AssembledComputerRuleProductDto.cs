using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssembledComputerRuleProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("quantity_free")]
        public int QuantityFree { get; set; }

        [JsonProperty("rule_id")]
        public int RuleId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("parent_category_id")]
        public int ParentCategoryId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("quantity_to_use")]
        public int? QuantityToUse { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }
    }
}