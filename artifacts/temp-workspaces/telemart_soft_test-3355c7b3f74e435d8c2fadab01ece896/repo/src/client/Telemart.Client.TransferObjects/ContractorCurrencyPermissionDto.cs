using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ContractorCurrencyPermissionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; set; }

        [JsonProperty("sale")]
        public bool Sale { get; set; }

        [JsonProperty("purchase")]
        public bool Purchase { get; set; }

        [JsonProperty("currency_control")]
        public bool CurrencyControl { get; set; }
    }
}