using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products.Catalog
{
    public sealed class QueryProductsCatalog : QueryEntitiesPagedRequestBase<ProductCatalogDto>
    {
        public QueryProductsCatalog(IFilteringItem filter)
            : base(filter, ApiResources.ProductsCatalog)
        {
        }
    }
}