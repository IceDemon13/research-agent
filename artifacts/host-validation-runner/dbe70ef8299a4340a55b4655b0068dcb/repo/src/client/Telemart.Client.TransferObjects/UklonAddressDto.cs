using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class UklonAddressDto
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }
    }
}