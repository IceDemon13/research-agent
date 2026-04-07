using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest
{
    public class QueryAssemblyTest : QueryEntityRequestBase<AssemblyTestDto>
    {
        public QueryAssemblyTest(object id)
            : base(ApiResources.AssemblyTests, id)
        {
        }
    }
}