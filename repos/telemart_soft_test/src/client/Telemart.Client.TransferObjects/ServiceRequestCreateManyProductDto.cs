using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestCreateManyProductDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("change_on_product_id")]
        public int? ChangeOnProductId { get; set; }

        [JsonProperty("bundle_id")]
        public int? BundleId { get; set; }

        [JsonProperty("sn")]
        public string SerialNumber { get; set; }

        [JsonProperty("defect")]
        public string Defect { get; set; }

        [JsonProperty("requirement_text")]
        public string RequirementText { get; set; }
    }
}