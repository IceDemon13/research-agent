using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class CreateProductDiscount : CreateEntityResultRequestBase<ProductCardDto, ProductDiscountCreateDto>
    {
        public CreateProductDiscount(int productId, ProductDiscountCreateDto dto)
            : base(dto, "products", productId, "discounts")
        {
        }
    }
}