using Newtonsoft.Json;

namespace Telemart.Client.PosTerminal.PrivatBank.Responses
{
    public abstract class ResponseItemBase
    {
        [JsonProperty("responseCode")]
        public string ResponseCode { get; set; }
    }
}
