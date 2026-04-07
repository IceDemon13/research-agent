using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Actions
{
    public sealed class LockContractor : LockRequestBase<ContractorDto>
    {
        public LockContractor(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Contractors, id)
        {
        }
    }
}