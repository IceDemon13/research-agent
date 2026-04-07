using Newtonsoft.Json;

namespace Telemart.Client.FiscalRegistrar.Entities
{
    public class FiscalRegistrarState
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("serial")]
        public string Serial { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("chkId")]
        public long ChkId { get; set; }

        [JsonProperty(nameof(JrnTime))]
        public long JrnTime { get; set; }

        [JsonProperty("currZ")]
        public long CurrZ { get; set; }

        [JsonProperty(nameof(IsWrk))]
        public long IsWrk { get; set; }

        [JsonProperty(nameof(Fiscalization))]
        public long Fiscalization { get; set; }

        [JsonProperty(nameof(FskMode))]
        public long FskMode { get; set; }

        [JsonProperty("CurrDI")]
        public long CurrDi { get; set; }

        [JsonProperty("ID_SAM")]
        public long IdSam { get; set; }

        [JsonProperty("ID_DEV")]
        public long IdDev { get; set; }

        [JsonProperty(nameof(NPrLin))]
        public long NPrLin { get; set; }

        [JsonProperty("err")]
        public dynamic Err { get; set; }
    }
}