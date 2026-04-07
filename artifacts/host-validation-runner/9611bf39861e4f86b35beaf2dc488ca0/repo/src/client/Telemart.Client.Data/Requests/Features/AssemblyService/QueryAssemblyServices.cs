using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService
{
    public sealed class QueryAssemblyServices : QueryEntitiesPagedRequestBase<AssemblyServiceDto>
    {
        public QueryAssemblyServices(IFilteringItem filter)
            : base(filter, ApiResources.AssemblyService)
        {
        }
    }
}