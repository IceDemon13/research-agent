using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class QueryServiceProducts : QueryEntitiesPagedRequestBase<ServiceProductDto>
    {
        public QueryServiceProducts(IFilteringItem filter)
            : base(filter, ApiResources.ServiceProducts)
        {
        }
    }
}
