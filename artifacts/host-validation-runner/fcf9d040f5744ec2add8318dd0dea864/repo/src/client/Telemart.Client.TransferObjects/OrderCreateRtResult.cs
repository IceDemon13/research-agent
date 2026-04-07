using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCreateRtResult
    {
        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}