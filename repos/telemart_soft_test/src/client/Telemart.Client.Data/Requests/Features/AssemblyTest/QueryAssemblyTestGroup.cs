using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest
{
    public class QueryAssemblyTestGroup : QueryEntityRequestBase<AssemblyTestGroupDto>
    {
        public QueryAssemblyTestGroup(object id)
            : base(ApiResources.AssemblyTests, "groups", id)
        {
        }
    }
}