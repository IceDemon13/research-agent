using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCanEditAdditionalServiceResponse
    {
        [JsonProperty("any_additional_services")]
        public bool AnyAdditionalServices { get; set; }
    }
}