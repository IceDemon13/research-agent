using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SuccessResponse
    {
        [JsonProperty("is_success")]
        public bool IsSuccess { get; set; }
    }
}