using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class StopAssemblyService : CallEntityActionRequestResultBase<AssemblyServiceDto>
    {
        public StopAssemblyService(int assemblyServiceId)
            : base(assemblyServiceId, ApiResources.AssemblyService, "stop")
        {
        }
    }
}