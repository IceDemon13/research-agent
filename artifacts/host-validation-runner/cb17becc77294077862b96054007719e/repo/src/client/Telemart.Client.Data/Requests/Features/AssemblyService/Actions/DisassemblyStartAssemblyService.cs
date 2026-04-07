using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class DisassemblyStartAssemblyService : CallEntityActionRequestResultBase<AssemblyServiceDto>
    {
        public DisassemblyStartAssemblyService(int assemblyServiceId)
            : base(assemblyServiceId, ApiResources.AssemblyService, "start_disassembly")
        {
        }
    }
}