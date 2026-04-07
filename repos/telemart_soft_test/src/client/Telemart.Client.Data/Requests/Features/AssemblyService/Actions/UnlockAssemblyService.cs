using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public class UnlockAssemblyService : UnlockRequestBase<AssemblyServiceDto>
    {
        public UnlockAssemblyService(int id, bool force = false)
            : base(force, ApiResources.AssemblyService, id)
        {
        }
    }
}