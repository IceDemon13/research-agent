using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Catalog;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductMoveData : CallEntityActionWithBodyRequestBase<ProductMovingDto, ProductMovingRequest>
    {
        public QueryProductMoveData(int productId, int categoryToId)
            : base(productId, new ProductMovingRequest(productId, categoryToId), ApiResources.Products, "query_move_data")
        {
        }
    }
}