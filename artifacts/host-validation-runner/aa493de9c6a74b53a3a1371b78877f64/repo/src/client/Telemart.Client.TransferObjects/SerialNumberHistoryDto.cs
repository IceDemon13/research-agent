using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SerialNumberHistoryDto
    {
        [JsonProperty("sn")]
        public string SerialNumber { get; set; }

        [JsonProperty("doc_type")]
        public string DocumentType { get; set; }

        [JsonProperty("doc_id")]
        public int DocumentId { get; set; }

        [JsonProperty("date_time")]
        public DateTime? DateTime { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }
    }
}