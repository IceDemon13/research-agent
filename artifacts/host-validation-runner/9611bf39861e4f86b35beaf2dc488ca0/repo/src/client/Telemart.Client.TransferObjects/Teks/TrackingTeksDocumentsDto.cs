using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Teks
{
    public sealed record TrackingTeksDocumentsDto
    {
        [JsonProperty("invoice_numbers")]
        public IReadOnlyCollection<string> InvoiceNumbers { get; init; }
    }
}