using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ValidationResultItemDto
    {
        [JsonProperty("msg")]
        public string Message { get; set; }

        [JsonProperty("is_error")]
        public bool IsError { get; set; }
    }
}