using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyFullRule;

namespace Telemart.Client.Data.Requests.Features.AssemblyFullRule.Actions
{
    public class LockAssemblyFullRule : LockRequestBase<AssemblyFullRuleDto>
    {
        public LockAssemblyFullRule(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.AssemblyFull, id)
        {
        }
    }
}