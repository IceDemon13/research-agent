using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class DefectAssemblyServiceProduct : CallEntityActionWithBodyRequestResultBase<AssemblyServiceDefectResultDto, DefectAssemblyServiceProduct.DefectAssemblyDto>
    {
        public DefectAssemblyServiceProduct(int assemblyId, int assemblyServiceProductId, string serialNumber, string statedDefect, bool createCall)
            : base(assemblyId, new DefectAssemblyDto(assemblyId, assemblyServiceProductId, serialNumber, statedDefect, createCall), ApiResources.AssemblyService, "defect")
        {
        }

        public class DefectAssemblyDto
        {
            public DefectAssemblyDto(int assemblyId, int assemblyProductId, string serialNumber, string statedDefect, bool createCall)
            {
                Id = assemblyId;
                AssemblyServiceProductId = assemblyProductId;
                SerialNumber = serialNumber;
                StatedDefect = statedDefect;
                CreateCall = createCall;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("assembly_product_id")]
            public int AssemblyServiceProductId { get; set; }

            [JsonProperty("serial_number")]
            public string SerialNumber { get; set; }

            [JsonProperty("stated_defect")]
            public string StatedDefect { get; set; }

            [JsonProperty("create_call")]
            public bool CreateCall { get; set; }
        }
    }
}