using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public class QueryProductsSimple : QueryEntitiesRequestBase<ProductSimpleDto>
    {
        public QueryProductsSimple(int[] ids)
            : base(new QueryProductsSimpleFilteringItem(ids), "products/simple")
        {
        }

        private class QueryProductsSimpleFilteringItem : FilteringItemBase
        {
            public QueryProductsSimpleFilteringItem(int[] ids)
            {
                Ids = ids;
            }

            [FilteringItemProperty("ids")]
            public int[] Ids { get; }
        }
    }
}