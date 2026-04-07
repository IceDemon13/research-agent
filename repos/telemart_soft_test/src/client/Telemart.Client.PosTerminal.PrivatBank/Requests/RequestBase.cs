using Newtonsoft.Json;

namespace Telemart.Client.PosTerminal.PrivatBank.Requests
{
    public abstract class RequestBase<TRequestParam, TResponseParam>
    {
        public RequestBase(string method, int step, TRequestParam parameter)
        {
            Method = method;
            Step = step;
            Parameter = parameter;
        }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("step")]
        public int Step { get; set; }

        [JsonProperty("params")]
        public TRequestParam Parameter { get; set; }
    }
}
