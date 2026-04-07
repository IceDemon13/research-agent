using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class CompleteAssemblyService : CallEntityActionRequestResultBase<AssemblyServiceDto>
    {
        public CompleteAssemblyService(int assemblyServiceId)
            : base(assemblyServiceId, ApiResources.AssemblyService, "complete")
        {
        }
    }
}