using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssembledComputerRuleCreateDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("modific")]
        public string Modific { get; set; }

        [JsonProperty("manufactor")]
        public string Manufactor { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("part_number")]
        public string PartNumber { get; set; }

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

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("base_product_id")]
        public int? BaseProductId { get; set; }

        [JsonProperty("ignore_slot_consumer_ids")]
        public int[] IgnoreSlotConsumerIds { get; set; }

        [JsonProperty("categories")]
        public List<AssembledComputerRuleCategoryCreateDto> Categories { get; set; }

        [JsonProperty("products")]
        public List<AssembledComputerRuleProductCreateDto> Products { get; set; }
    }
}