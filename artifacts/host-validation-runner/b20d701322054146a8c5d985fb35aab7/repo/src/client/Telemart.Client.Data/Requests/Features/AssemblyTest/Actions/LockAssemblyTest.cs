using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest.Actions
{
    public class LockAssemblyTest : LockRequestBase<AssemblyTestDto>
    {
        public LockAssemblyTest(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.AssemblyTests, id)
        {
        }
    }
}