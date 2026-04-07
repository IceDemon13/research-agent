using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Ukrposhta
{
    public sealed class UpDocumentDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("send_date")]
        public DateTime? SendDate { get; set; }

        [JsonProperty("arrival_date")]
        public DateTime? ArrivalDate { get; set; }

        [JsonProperty("receive_date")]
        public DateTime? ReceiveDate { get; set; }

        [JsonProperty("base_64_pdf")]
        public string Base64Pdf { get; set; }
    }
}