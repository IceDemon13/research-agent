using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssembledComputerRuleProductCreateDto
    {
        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity_to_use")]
        public int? QuantityToUse { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }
    }
}