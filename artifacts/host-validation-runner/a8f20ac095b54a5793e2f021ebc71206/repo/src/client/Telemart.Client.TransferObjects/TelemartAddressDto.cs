using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record TelemartAddressDto
    {
        [JsonProperty("address")]
        public string Address { get; init; }

        [JsonProperty("phone")]
        public string Phone { get; init; }

        [JsonProperty("email")]
        public string Email { get; init; }
    }
}