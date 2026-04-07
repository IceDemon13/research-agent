using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.BankPayment
{
    public class BankPaymentDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
        public int? OrderId { get; set; }

        [JsonProperty("parsed_order_id")]
        public int? ParsedOrderId { get; set; }

        [JsonProperty("paid_on")]
        public DateTime PaidOn { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; set; }

        [JsonProperty("contractor_name")]
        public string ContractorName { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("last_error")]
        public string LastError { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; set; }
    }
}