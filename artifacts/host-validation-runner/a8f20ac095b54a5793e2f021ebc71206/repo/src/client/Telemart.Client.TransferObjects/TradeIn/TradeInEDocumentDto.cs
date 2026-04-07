using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInEDocumentDto : TradeInEDocumentSimpleDto
    {
        [JsonProperty("document_type_id")]
        public int DocumentTypeId { get; init; }

        [JsonProperty("document_name")]
        public string DocumentName { get; init; }

        [JsonProperty("download_on")]
        public DateTime DownloadOn { get; init; }

        [JsonProperty("bytes")]
        public byte[] Bytes { get; init; }
    }
}