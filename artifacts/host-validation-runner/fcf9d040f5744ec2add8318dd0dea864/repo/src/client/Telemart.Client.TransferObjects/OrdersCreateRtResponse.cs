using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrdersCreateRtResponse
    {
        [JsonProperty("data")]
        public List<OrderCreateRtResult> Data { get; set; }
    }
}