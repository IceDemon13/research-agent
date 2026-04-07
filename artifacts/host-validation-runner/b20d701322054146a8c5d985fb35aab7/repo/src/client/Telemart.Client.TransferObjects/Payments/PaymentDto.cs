using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Payments
{
    public class PaymentDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("name_ua")]
        public string NameUa { get; init; }

        [JsonProperty("name_en")]
        public string NameEn { get; init; }

        [JsonProperty("refund_payment_ids")]
        public int[] RefundPaymentIds { get; init; }

        [JsonProperty("refund_requisites_control")]
        public bool RefundRequisitesControl { get; init; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; init; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; init; }

        [JsonProperty("auto_fill_sources")]
        public bool AutoFillSources { get; init; }

        [JsonProperty("only_create_on_web")]
        public bool OnlyCreateOnWeb { get; init; }

        [JsonProperty("sms_template")]
        public bool SmsTemplate { get; init; }

        [JsonProperty("fee")]
        public decimal Fee { get; init; }

        [JsonProperty("provider_fee")]
        public decimal ProviderFee { get; init; }

        [JsonProperty("credit")]
        public bool Credit { get; init; }

        [JsonProperty("partial_credit")]
        public bool PartialCredit { get; init; }

        [JsonProperty("can_edit_products")]
        public bool CanEditProducts { get; init; }

        [JsonProperty("limit_uah")]
        public int LimitUah { get; init; }

        [JsonProperty("min_limit_uah")]
        public int? MinLimitUah { get; init; }

        [JsonProperty("limit_usd")]
        public int LimitUsd { get; init; }

        [JsonProperty("refund_revert_allowed")]
        public bool RefundRevertAllowed { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("fiscal")]
        public bool Fiscal { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}