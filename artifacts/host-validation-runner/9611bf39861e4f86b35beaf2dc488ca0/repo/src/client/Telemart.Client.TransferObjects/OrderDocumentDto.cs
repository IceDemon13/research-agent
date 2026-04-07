using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record OrderDocumentDto : OrderDocumentSimpleDto
    {
        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}