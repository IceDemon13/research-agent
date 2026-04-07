using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyServiceProduct
{
    public class QueryAssemblyServiceProducts : QueryEntitiesRequestBase<AssemblyServiceProductDto>
    {
        public QueryAssemblyServiceProducts(IFilteringItem filter)
            : base(filter, ApiResources.AssemblyServiceProducts)
        {
        }
    }
}