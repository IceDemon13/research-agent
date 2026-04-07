using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class WorkPlaceTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("is_virtual")]
        public bool IsVirtual { get; init; }
    }
}