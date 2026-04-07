using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PosTerminal
{
    public sealed class CreatePosSettingsDto
    {
        public CreatePosSettingsDto(
            int posTypeId,
            int cashboxId,
            int? legalEntityId,
            string merchant,
            string uniqueDeviceId,
            string macAddress,
            string ipAddress,
            bool force = false)
        {
            PosTypeId = posTypeId;
            CashboxId = cashboxId;
            LegalEntityId = legalEntityId;
            Merchant = merchant;
            UniqueDeviceId = uniqueDeviceId;
            MacAddress = macAddress;
            IpAddress = ipAddress;
            Force = force;
        }

        [JsonProperty("id_pos_type")]
        public int PosTypeId { get; set; }

        [JsonProperty("id_cashbox")]
        public int CashboxId { get; set; }

        [JsonProperty("id_legal_entity")]
        public int? LegalEntityId { get; set; }

        [JsonProperty("merchant")]
        public string Merchant { get; set; }

        [JsonProperty("id_unique_device")]
        public string UniqueDeviceId { get; set; }

        [JsonProperty("ip_address")]
        public string IpAddress { get; set; }

        [JsonProperty("mac_address")]
        public string MacAddress { get; set; }

        [JsonProperty("force")]
        public bool Force { get; set; }
    }
}