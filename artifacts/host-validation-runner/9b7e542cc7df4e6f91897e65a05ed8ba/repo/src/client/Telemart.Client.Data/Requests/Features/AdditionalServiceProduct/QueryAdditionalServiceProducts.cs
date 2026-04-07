using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct
{
    public sealed class QueryAdditionalServiceProducts : QueryEntitiesPagedRequestBase<AdditionalServiceProductDto>
    {
        public QueryAdditionalServiceProducts(IFilteringItem filter)
            : base(filter, ApiResources.AdditionalServicesProducts)
        {
        }
    }
}