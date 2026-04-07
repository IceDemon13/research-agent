using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductDefectResultDtoBase
    {
        [JsonProperty("service_request")]
        public ServiceRequestDto ServiceRequest { get; set; }
    }
}