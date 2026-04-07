using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public class OutcomingCallDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("task")]
        public string Task { get; set; }

        [JsonProperty("call_later")]
        public bool CallLater { get; set; }

        [JsonProperty("dependencies")]
        public CallDependencyDto[] Dependencies { get; set; }

        [JsonProperty("call_from")]
        public DateTime? CallFrom { get; set; }

        [JsonProperty("call_to")]
        public DateTime? CallTo { get; set; }
    }
}