using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Teks
{
    public class TtnStatusDto
    {
        [JsonProperty("invoice_number")]
        public string InvoiceNumber { get; init; }

        [JsonProperty("status_date_time")]
        public string StatusDateTime { get; init; }

        [JsonProperty("status_name")]
        public string StatusName { get; init; }

        [JsonProperty("status_code")]
        public string StatusCode { get; init; }
    }
}