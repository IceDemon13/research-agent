using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class LockAdditionalServiceGroup : LockRequestBase<AdditionalServiceGroupDto>
    {
        public LockAdditionalServiceGroup(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.AdditionalServicesGroups, id)
        {
        }
    }
}