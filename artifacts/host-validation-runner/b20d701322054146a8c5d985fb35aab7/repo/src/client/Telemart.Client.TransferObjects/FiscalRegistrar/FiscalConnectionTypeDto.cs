using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.FiscalRegistrar
{
    public sealed class FiscalConnectionTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}