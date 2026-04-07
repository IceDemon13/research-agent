using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.MeestExpress
{
    public class MeDocumentDto
    {
        [JsonProperty("ttn")]
        public string Ttn { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }
    }
}