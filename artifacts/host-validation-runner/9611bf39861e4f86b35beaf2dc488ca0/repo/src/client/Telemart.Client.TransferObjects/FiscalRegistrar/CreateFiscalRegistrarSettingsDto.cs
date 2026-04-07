using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.FiscalRegistrar
{
    public sealed class CreateFiscalRegistrarSettingsDto
    {
        public CreateFiscalRegistrarSettingsDto(
            int fiscalConnectionTypeId,
            int cashboxId,
            int? legalEntityId,
            string login,
            string password,
            string uniqueDeviceId,
            string ipAddress,
            string macAddress)
        {
            FiscalConnectionTypeId = fiscalConnectionTypeId;
            CashboxId = cashboxId;
            LegalEntityId = legalEntityId;
            Login = login;
            Password = password;
            UniqueDeviceId = uniqueDeviceId;
            IpAddress = ipAddress;
            MacAddress = macAddress;
        }

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
    }
}