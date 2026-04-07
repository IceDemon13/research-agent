using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class IsProductRemovedDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("requirement_id")]
        public int Requirement { get; set; }

        [JsonProperty("old_requirement_id")]
        public int? OldRequirement { get; set; }
    }
}