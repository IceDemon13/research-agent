using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ProductCompatibility;

namespace Telemart.Client.Data.Requests.Features.ProductCompatibility
{
    public class UpdateProductCompatibility : UpdateEntityResultRequestBase<ProductCompatibilityDto, ProductCompatibilitySaveDto>
    {
        public UpdateProductCompatibility(ProductCompatibilitySaveDto dto)
            : base(dto, ApiResources.ProductCompatibilities, dto.Id)
        {
        }
    }
}
