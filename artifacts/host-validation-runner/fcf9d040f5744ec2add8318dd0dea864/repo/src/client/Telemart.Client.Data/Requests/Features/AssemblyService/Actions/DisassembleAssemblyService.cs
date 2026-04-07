using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public class DisassembleAssemblyService : CallEntityActionRequestResultBase<AssemblyServiceDto>
    {
        public DisassembleAssemblyService(int assemblyServiceId)
        : base(assemblyServiceId, ApiResources.AssemblyService, "disassembled")
        {
        }
    }
}