using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestTakeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("apppearance")]
        public string Appearance { get; set; }

        [JsonProperty("inspection")]
        public string Inspection { get; set; }

        [JsonProperty("completeness_comment")]
        public string CompletenessComment { get; set; }

        [JsonProperty("sn")]
        public string SerialNumber { get; set; }

        [JsonProperty("warehouse_location_id")]
        public int WarehouseLocationId { get; set; }

        [JsonProperty("product_additional_service_ids")]
        public List<int> ProductAdditionalServiceIds { get; set; }

        [JsonProperty("requirement_text")]
        public string RequirementText { get; set; }

        [JsonProperty("payment_id")]
        public int? PaymentId { get; set; }

        [JsonProperty("repair_days")]
        public int? RepairDays { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}