using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CancelUklonOrderDto
    {
        [JsonProperty("id")]
        public int OrderId { get; init; }
    }
}