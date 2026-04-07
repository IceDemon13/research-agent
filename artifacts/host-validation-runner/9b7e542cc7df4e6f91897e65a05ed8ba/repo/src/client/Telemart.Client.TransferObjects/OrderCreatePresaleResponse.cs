using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCreatePresaleResponse
    {
        [JsonProperty("order")]
        public OrderDto Order { get; set; }

        [JsonProperty("service_request")]
        public ServiceRequestDto ServiceRequest { get; set; }
    }
}