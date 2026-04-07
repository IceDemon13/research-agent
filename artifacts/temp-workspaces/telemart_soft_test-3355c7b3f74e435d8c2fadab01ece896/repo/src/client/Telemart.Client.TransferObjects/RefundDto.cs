using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class RefundDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("order_client_id")]
        public int OrderClientId { get; set; }

        [JsonProperty("order_subdivision_id")]
        public int OrderSubdivisionId { get; set; }

        [JsonProperty("service_request_id")]
        public int? ServiceRequestId { get; set; }

        [JsonProperty("payment_id")]
        public int? PaymentId { get; set; }

        [JsonProperty("legal_entity")]
        public LegalEntityDto LegalEntity { get; set; }

        [JsonProperty("completed_on_fiscal_registrar")]
        public bool CompletedOnFiscalRegistrar { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("middle_name")]
        public string MiddleName { get; set; }

        [JsonProperty("iban")]
        public string Iban { get; set; }

        [JsonProperty("inn")]
        public string Inn { get; set; }

        [JsonProperty("card_number")]
        public string CardNumber { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("cashbox_id")]
        public int? CashboxId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("approved_on")]
        public DateTime? ApprovedOn { get; set; }

        [JsonProperty("approved_by")]
        public int? ApprovedBy { get; set; }

        [JsonProperty("payed_on")]
        public DateTime? PayedOn { get; set; }

        [JsonProperty("payed_by")]
        public int? PayedBy { get; set; }

        [JsonProperty("fiscal_id")]
        public string FiscalId { get; set; }

        [JsonProperty("order_payment_id")]
        public int? OrderPaymentId { get; set; }
    }
}