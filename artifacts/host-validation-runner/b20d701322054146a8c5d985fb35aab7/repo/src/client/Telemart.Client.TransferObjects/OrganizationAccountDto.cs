using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrganizationAccountDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("organization_id")]
        public int OrganizationId { get; set; }

        [JsonProperty("bank_id")]
        public int BankId { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("id_cashbox")]
        public int CashboxId { get; set; }

        [JsonProperty("is_default")]
        public bool IsDefault { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("payments")]
        public List<int> Payments { get; set; }
    }
}