using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestDiscussionCreateDto
    {
        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; set; }

        [JsonProperty("contractor_contact_id")]
        public int? ContractorContactId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}