using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record AdditionalServiceProductConsumableSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("scanned")]
        public bool Scanned { get; init; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; init; }

        [JsonProperty("guest_product")]
        public GuestProductDto GuestProduct { get; set; }
    }
}