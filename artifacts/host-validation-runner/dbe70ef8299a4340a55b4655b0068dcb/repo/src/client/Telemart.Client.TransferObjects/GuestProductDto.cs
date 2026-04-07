using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record GuestProductDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("keep_product")]
        public bool KeepProduct { get; set; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}