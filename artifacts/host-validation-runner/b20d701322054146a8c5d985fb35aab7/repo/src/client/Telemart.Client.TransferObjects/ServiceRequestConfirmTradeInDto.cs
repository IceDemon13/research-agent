using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestConfirmTradeInDto
    {
        public ServiceRequestConfirmTradeInDto(int serviceRequestId, string comment)
        {
            Comment = comment;
            ServiceRequestId = serviceRequestId;
        }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; set; }
    }
}