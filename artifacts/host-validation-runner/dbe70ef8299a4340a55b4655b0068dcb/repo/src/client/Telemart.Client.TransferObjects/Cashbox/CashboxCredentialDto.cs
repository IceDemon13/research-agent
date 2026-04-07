using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Cashbox
{
    public sealed class CashboxCredentialDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; set; }

        [JsonProperty("provider_id")]
        public int ProviderId { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}