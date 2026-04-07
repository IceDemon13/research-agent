using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceCenter
{
    public sealed class LockServiceCenter : LockRequestBase<ServiceCenterDto>
    {
        public LockServiceCenter(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.ServiceCenters, id)
        {
        }
    }
}
