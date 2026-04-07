using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NewPostDocumentDto
    {
        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("recipient_date_time")]
        public string RecipientDateTime { get; set; }

        [JsonProperty("scheduled_delivery_date")]
        public string ScheduledDeliveryDate { get; set; }

        [JsonProperty("recipient_full_name")]
        public string RecipientFullName { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("status_code")]
        public string StatusCode { get; set; }
    }
}