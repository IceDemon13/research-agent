using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public class LockAssemblyService : LockRequestBase<AssemblyServiceDto>
    {
        public LockAssemblyService(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.AssemblyService, id)
        {
        }
    }
}