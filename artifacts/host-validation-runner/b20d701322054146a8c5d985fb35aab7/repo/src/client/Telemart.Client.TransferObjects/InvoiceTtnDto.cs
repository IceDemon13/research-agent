using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceTtnDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("ttn")]
        public string Ttn { get; set; }
    }
}