using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ScanSheetCreateResponse
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("base_64_pdf")]
        public string Base64Pdf { get; set; }
    }
}