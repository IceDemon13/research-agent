using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssembledComputerRuleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("brand")]
        public CategoryDto Brand { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("configuration")]
        public string Configuration { get; set; }

        [JsonProperty("series")]
        public string Series { get; set; }

        [JsonProperty("line")]
        public string Line { get; set; }

        [JsonProperty("part_number")]
        public string PartNumber { get; set; }

        [JsonProperty("modific")]
        public string Modific { get; set; }

        [JsonProperty("group_name")]
        public string GroupName { get; set; }

        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("prefix_ukr")]
        public string PrefixUkr { get; set; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("color_primary_id")]
        public int? ColorPrimaryId { get; set; }

        [JsonProperty("color_secondary_id")]
        public int? ColorSecondaryId { get; set; }

        [JsonProperty("base_product_id")]
        public int? BaseProductId { get; set; }

        [JsonProperty("base_product_name")]
        public string BaseProductName { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("use_any_products")]
        public bool UseAnyProducts { get; set; }

        [JsonProperty("ignore_slot_consumer_ids")]
        public int[] IgnoreSlotConsumerIds { get; set; }

        [JsonProperty("categories")]
        public List<AssembledComputerRuleCategoryDto> Categories { get; set; }

        [JsonProperty("products")]
        public List<AssembledComputerRuleProductDto> Products { get; set; }
    }
}