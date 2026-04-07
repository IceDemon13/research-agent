using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Catalog;

namespace Telemart.Client.Data.Requests.Features.Products.Actions
{
    public sealed class SearchProductsCatalog : CallActionWithBodyRequestBase<ProductsCatalogSearchResponse, ProductCatalogSearchRequest>
    {
        public SearchProductsCatalog(ProductCatalogSearchRequest productSearchRequest)
            : base(productSearchRequest, ApiResources.Products, "search_catalog")
        {
        }
    }
}