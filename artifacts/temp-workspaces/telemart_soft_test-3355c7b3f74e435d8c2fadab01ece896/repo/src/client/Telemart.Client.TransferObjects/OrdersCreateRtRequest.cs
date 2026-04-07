using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrdersCreateRtRequest
    {
        [JsonProperty("order_ids")]
        public int[] OrderIds { get; set; }
    }
}