using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Gabaritka
{
    public sealed class GabaritkaDocumentDto
    {
        [JsonProperty("ttn")]
        public string Ttn { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}