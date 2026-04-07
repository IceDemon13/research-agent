using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyFullRule;

namespace Telemart.Client.Data.Requests.Features.AssemblyFullRule.Actions
{
    public class UnlockAssemblyFullRule : UnlockRequestBase<AssemblyFullRuleDto>
    {
        public UnlockAssemblyFullRule(int id, bool force = false)
            : base(force, ApiResources.AssemblyFull, id)
        {
        }
    }
}