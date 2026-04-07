using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public class CallIntervalRequest
    {
        public CallIntervalRequest(int? callTypeId, int attempt)
        {
            CallTypeId = callTypeId;
            Attempt = attempt;
        }

        [JsonProperty("call_type_id")]
        public int? CallTypeId { get; set; }

        [JsonProperty("attempt")]
        public int Attempt { get; set; }
    }
}