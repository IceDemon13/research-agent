using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Teks
{
    public sealed record TeksDocumentDto
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("status")]
        public string Status { get; init; }

        [JsonProperty("sender_fio")]
        public string SenderFio { get; init; }

        [JsonProperty("sender_city_id")]
        public int? SenderCityId { get; init; }

        [JsonProperty("recipient_fio")]
        public string RecipientFio { get; init; }

        [JsonProperty("recipient_city_id")]
        public int? RecipientCityId { get; init; }

        [JsonProperty("send_date")]
        public DateTime? SendDate { get; init; }

        [JsonProperty("arrival_date")]
        public DateTime? ArrivalDate { get; init; }

        [JsonProperty("receive_date")]
        public DateTime? ReceiveDate { get; init; }

        [JsonProperty("create_on")]
        public DateTime CreatedOn { get;  set; }

        [JsonProperty("byte_pdf")]
        public byte[] BytePdf { get; set; }
    }
}