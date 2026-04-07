using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Catalog;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class MoveProduct : CallEntityActionWithBodyRequestResultBase<ProductCardDto, ProductMoveDto>
    {
        public MoveProduct(ProductMoveDto dto)
            : base(dto.Id, dto, ApiResources.Products, "move")
        {
        }
    }
}