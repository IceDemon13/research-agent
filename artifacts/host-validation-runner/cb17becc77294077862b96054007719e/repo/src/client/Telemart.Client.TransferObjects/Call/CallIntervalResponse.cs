using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public class CallIntervalResponse
    {
        [JsonProperty("from")]
        public DateTime Form { get; set; }

        [JsonProperty("to")]
        public DateTime To { get; set; }
    }
}