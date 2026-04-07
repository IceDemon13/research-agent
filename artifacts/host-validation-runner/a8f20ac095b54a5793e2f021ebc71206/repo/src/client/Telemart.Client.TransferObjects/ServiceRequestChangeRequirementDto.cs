using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestChangeRequirementDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("requirement_id")]
        public int Requirement { get; set; }

        [JsonProperty("repair_days")]
        public int? RepairDays { get; set; }

        [JsonProperty("requirement_text")]
        public string RequirementText { get; set; }

        [JsonProperty("requirement_payment_id")]
        public int? RequirementPaymentId { get; set; }

        [JsonProperty("requirement_cashbox_id")]
        public int? RequirementCashboxId { get; set; }

        [JsonProperty("product_new_id")]
        public int? ProductNewId { get; set; }

        [JsonProperty("requisites")]
        public RefundRequisitesDto Requisites { get; set; }
    }
}