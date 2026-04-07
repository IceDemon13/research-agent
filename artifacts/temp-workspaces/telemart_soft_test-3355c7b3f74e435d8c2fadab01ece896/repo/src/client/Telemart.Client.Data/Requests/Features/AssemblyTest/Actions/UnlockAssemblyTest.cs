using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest.Actions
{
    public class UnlockAssemblyTest : UnlockRequestBase<AssemblyTestDto>
    {
        public UnlockAssemblyTest(int id, bool force = false)
            : base(force, ApiResources.AssemblyTests, id)
        {
        }
    }
}