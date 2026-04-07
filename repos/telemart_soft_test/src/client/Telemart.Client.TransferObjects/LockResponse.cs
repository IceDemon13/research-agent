using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class LockResponse<TEntity>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("dto")]
        public TEntity Dto { get; set; }
    }
}