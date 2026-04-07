using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.TransferObjects.Cashbox
{
    public class CashboxDto : TrackableDtoBase<int>
    {
        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("legal_entity_id")]
        public int? LegalEntityId { get; set; }

        [JsonProperty("credential_id")]
        public int? CredentialId { get; set; }

        [JsonProperty("strongbox_id")]
        public int? StrongboxId { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [JsonProperty("payment_account")]
        public string PaymentAccount { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("min_fee")]
        public decimal MinFee { get; set; }

        [JsonProperty("incombustible_amount")]
        public decimal? IncombustibleAmount { get; set; }

        [JsonProperty("auto_pay")]
        public bool AutoPay { get; set; }

        [JsonProperty("active")]
        public bool IsActive { get; set; }

        [JsonProperty("has_account")]
        public bool HasAccount { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("session")]
        public CashboxSessionDto Session { get; set; }

        [JsonProperty("legal_entity")]
        public LegalEntityDto LegalEntity { get; set; }

        [JsonProperty("allowed_payments")]
        public IReadOnlyCollection<int> AllowedPayments { get; set; }
    }
}