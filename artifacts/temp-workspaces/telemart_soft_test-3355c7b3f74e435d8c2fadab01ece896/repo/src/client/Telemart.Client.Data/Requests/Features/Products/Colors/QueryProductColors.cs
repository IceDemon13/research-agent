using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products.Colors
{
    public sealed class QueryProductColors : QueryEntitiesPagedRequestBase<ProductColorDto>
    {
        public QueryProductColors()
            : base(ApiResources.ProductColors)
        {
        }
    }
}
