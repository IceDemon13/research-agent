using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public sealed class UpHouseDto
    {
        [JsonProperty("house_number")]
        public string HouseNumber { get; set; }

        [JsonProperty("index")]
        public string Index { get; set; }
    }
}