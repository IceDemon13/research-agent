using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductsAttributesByIds : QueryEntitiesPagedRequestBase<ProductAttributesDto>
    {
        public QueryProductsAttributesByIds(int[] ids, bool includeSn)
            : base(new ProductsAttributesFilteringItem(ids, includeSn), ApiResources.Products, "attributes/by_ids")
        {
        }

        private sealed class ProductsAttributesFilteringItem : FilteringItemBase
        {
            public ProductsAttributesFilteringItem(int[] ids, bool includeSn)
            {
                Ids = ids;
                IncludeSn = includeSn;
            }

            [FilteringItemProperty("ids")]
            public int[] Ids { get; }

            [FilteringItemProperty("includeSn")]
            public bool IncludeSn { get; }
        }
    }
}