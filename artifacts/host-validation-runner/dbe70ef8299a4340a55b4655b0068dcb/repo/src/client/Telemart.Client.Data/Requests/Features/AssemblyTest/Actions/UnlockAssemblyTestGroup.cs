using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest.Actions
{
    public class UnlockAssemblyTestGroup : UnlockRequestBase<AssemblyTestGroupDto>
    {
        public UnlockAssemblyTestGroup(int id, bool force = false)
            : base(force, $"{ApiResources.AssemblyTests}/groups", id)
        {
        }
    }
}