using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PosTerminal
{
    public sealed class PosSettingsDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

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

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int? ModifiedBy { get; set; }
    }
}