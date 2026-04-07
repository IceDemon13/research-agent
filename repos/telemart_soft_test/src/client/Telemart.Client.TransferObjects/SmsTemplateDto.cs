using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SmsTemplateDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sms_text")]
        public string SmsText { get; set; }

        [JsonProperty("viber_text")]
        public string ViberText { get; set; }

        [JsonProperty("legal_entity_id")]
        public int? LegalEntityId { get; set; }

        [JsonProperty("show_in_order")]
        public bool ShowInOrder { get; set; }

        [JsonProperty("show_in_service_request")]
        public bool ShowInServiceRequest { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}