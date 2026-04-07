using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AssemblyService
{
    public class AssemblyServiceDefectResultDto : ProductDefectResultDtoBase
    {
        [JsonProperty("assembly_service")]
        public AssemblyServiceDto AssemblyService { get; set; }
    }
}