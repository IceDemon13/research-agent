using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest.Actions
{
    public class LockAssemblyTestGroup : LockRequestBase<AssemblyTestGroupDto>
    {
        public LockAssemblyTestGroup(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.AssemblyTests, "groups", id)
        {
        }
    }
}