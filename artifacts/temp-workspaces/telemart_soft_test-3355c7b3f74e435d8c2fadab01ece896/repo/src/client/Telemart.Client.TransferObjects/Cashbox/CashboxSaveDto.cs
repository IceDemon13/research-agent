using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Cashbox
{
    public class CashboxSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("credential_id")]
        public int? CredentialId { get; set; }

        [JsonProperty("legal_entity_id")]
        public int? LegalEntityId { get; set; }

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

        [JsonProperty("is_create_employee_cashbox")]
        public bool IsCreateEmployeeCashbox { get; set; }

        [JsonProperty("allowed_payments")]
        public IReadOnlyCollection<int> AllowedPayments { get; set; }
    }
}