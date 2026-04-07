using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record InvoiceAnalyzeResultItemDto
    {
        [JsonProperty("is_error")]
        public bool IsError { get; init; }

        [JsonProperty("text")]
        public string Text { get; init; }
    }
}