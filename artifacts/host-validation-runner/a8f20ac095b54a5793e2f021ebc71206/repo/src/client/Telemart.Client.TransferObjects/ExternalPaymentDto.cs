using System;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.WorkSchedule;

namespace Telemart.Client.TransferObjects
{
    public sealed record ExternalPaymentDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; init; }

        [JsonProperty("payment_can_edit_products")]
        public bool PaymentCanEditProducts { get; init; }

        [JsonProperty("payment_state_id")]
        public int PaymentStateId { get; init; }

        [JsonProperty("external_order_id")]
        public string ExternalOrderId { get; init; }

        [JsonProperty("created_amount")]
        public decimal CreatedAmount { get; init; }

        [JsonProperty("holded_amount")]
        public decimal? HoldedAmount { get; init; }

        [JsonProperty("received_amount")]
        public decimal? ReceivedAmount { get; init; }

        [JsonProperty("link")]
        public string Link { get; init; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; init; }

        [JsonProperty("parent_payment_id")]
        public int? ParentPaymentId { get; init; }

        [JsonProperty("params")]
        public ExternalPaymentParamsDto Params { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}