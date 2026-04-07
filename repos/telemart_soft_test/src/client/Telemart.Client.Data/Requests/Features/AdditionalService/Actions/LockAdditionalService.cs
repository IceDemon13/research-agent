using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class LockAdditionalService : LockRequestBase<AdditionalServiceDto>
    {
        public LockAdditionalService(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.AdditionalServices, id)
        {
        }
    }
}