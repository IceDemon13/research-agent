using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceRequestSetServiceCenterDto
    {
        [JsonProperty("service_center_id")]
        public int ServiceCenterId { get; set; }
    }
}