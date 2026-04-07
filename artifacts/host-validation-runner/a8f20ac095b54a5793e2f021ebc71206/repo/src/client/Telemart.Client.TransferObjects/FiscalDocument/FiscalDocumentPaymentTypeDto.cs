using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.FiscalDocument
{
    public class FiscalDocumentPaymentTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}