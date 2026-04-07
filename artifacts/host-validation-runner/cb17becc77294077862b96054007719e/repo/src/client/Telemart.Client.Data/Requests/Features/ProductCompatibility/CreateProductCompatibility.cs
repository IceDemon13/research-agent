using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ProductCompatibility;

namespace Telemart.Client.Data.Requests.Features.ProductCompatibility
{
    public class CreateProductCompatibility : CreateEntityResultRequestBase<ProductCompatibilityDto, ProductCompatibilitySaveDto>
    {
        public CreateProductCompatibility(ProductCompatibilitySaveDto dto)
            : base(dto, ApiResources.ProductCompatibilities)
        {
        }
    }
}
