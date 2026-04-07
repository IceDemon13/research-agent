using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class UpdateProductCard : UpdateEntityResultRequestBase<ProductCardDto, ProductCardSaveDto>
    {
        public UpdateProductCard(ProductCardSaveDto dto)
            : base(dto, ApiResources.Products, dto.ProductId, "card")
        {
        }
    }
}