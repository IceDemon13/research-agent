using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PosTerminal
{
    public sealed class PosTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}