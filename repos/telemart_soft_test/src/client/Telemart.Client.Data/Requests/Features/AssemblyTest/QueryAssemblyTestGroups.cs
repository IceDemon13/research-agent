using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyTest
{
    public sealed class QueryAssemblyTestGroups : QueryEntitiesRequestBase<AssemblyTestGroupDto>
    {
        public QueryAssemblyTestGroups()
            : base($"{ApiResources.AssemblyTests}/groups")
        {
        }
    }
}