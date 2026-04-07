using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class StartTestingAssemblyService : CallEntityActionRequestResultBase<AssemblyServiceDto>
    {
        public StartTestingAssemblyService(int assemblyServiceId)
            : base(assemblyServiceId, ApiResources.AssemblyService, "start_testing")
        {
        }
    }
}