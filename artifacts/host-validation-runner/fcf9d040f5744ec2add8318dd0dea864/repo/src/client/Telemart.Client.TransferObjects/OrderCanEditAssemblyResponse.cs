using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCanEditAssemblyResponse
    {
        [JsonProperty("any_assembly_services")]
        public bool AnyAssemblyServices { get; set; }
    }
}