using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceComparisonSaveDto
    {
        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("comparison_time")]
        public TimeSpan ComparisonTime { get; set; }

        [JsonProperty("products")]
        public List<InvoiceProductComparisonDto> Products { get; set; }
    }
}