using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.FiscalRegistrar
{
    public sealed class FiscalRegistrarSettingsDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("id_fiscal_connection_type")]
        public int FiscalConnectionTypeId { get; set; }

        [JsonProperty("id_cashbox")]
        public int CashboxId { get; set; }

        [JsonProperty("id_legal_entity")]
        public int? LegalEntityId { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("id_unique_device")]
        public string UniqueDeviceId { get; set; }

        [JsonProperty("ip_address")]
        public string IpAddress { get; set; }

        [JsonProperty("mac_address")]
        public string MacAddress { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("selected")]
        public bool Selected { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int? ModifiedBy { get; set; }

        [JsonProperty("session_is_open")]
        public bool SessionIsOpen { get; set; }
    }
}