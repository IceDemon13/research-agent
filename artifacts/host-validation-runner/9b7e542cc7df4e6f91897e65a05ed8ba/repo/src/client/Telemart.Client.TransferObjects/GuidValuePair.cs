using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class GuidValuePair<TValue>
        where TValue : class, new()
    {
        [JsonProperty("key")]
        public Guid Key { get; set; }

        [JsonProperty("value")]
        public TValue Value { get; set; }
    }
}